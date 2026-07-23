using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WholeCareInsurance.api.Data;
using WholeCareInsurance.Migration.Importers;
using WholeCareInsurance.Migration.Matching;
using WholeCareInsurance.Migration.Reporting;

namespace WholeCareInsurance.Migration
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var options = CliOptions.Parse(args);
            if (options == null) return 1;

            Console.WriteLine($"Modo: {(options.Commit ? "--commit" : "--dry-run")}");
            Console.WriteLine($"Origen: {options.SourceDir}");
            Console.WriteLine($"Ventana de historial: {options.HistoryWindowDays} días");
            Console.WriteLine();

            var connectionString = ResolveConnectionString(options);
            var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                // Default de 30s se queda corto: --dry-run mantiene una única transacción
                // abierta con cientos de savepoints hasta el final del run, y el ROLLBACK
                // final de todo eso puede tardar más que el default.
                .UseSqlServer(connectionString, sql => sql.CommandTimeout(600))
                .Options;

            await using var db = new AppDbContext(dbOptions);
            await db.Database.OpenConnectionAsync();

            var report = new MigrationReport
            {
                Mode = options.Commit ? "commit" : "dry-run",
                HistoryWindowDays = options.HistoryWindowDays,
            };

            try
            {
                if (options.Commit)
                    await RunCommitAsync(db, options, report);
                else
                    await RunDryRunAsync(db, options, report);
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
            }

            report.Print();
            var reportPath = Path.Combine(Directory.GetCurrentDirectory(),
                $"migration-report-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            report.SaveJson(reportPath);
            Console.WriteLine($"\nReporte guardado en: {reportPath}");

            return 0;
        }

        // --dry-run: TODO corre dentro de una única transacción externa que se
        // hace Rollback incondicional al final (nunca se persiste nada). Cada grupo de
        // póliza usa un SAVEPOINT propio para que un error puntual no deje la
        // transacción entera en estado "aborted" para el resto de la simulación.
        private static async Task RunDryRunAsync(AppDbContext db, CliOptions options, MigrationReport report)
        {
            await using var transaction = await db.Database.BeginTransactionAsync();

            var matcher = new EntityMatcher(db, report);
            await matcher.PreloadCachesAsync();
            var pipeline = new ImportPipeline(db, matcher, report, options.HistoryWindowDays);

            var allGroups = await PrepareAllFilesAsync(options, pipeline, report);

            var savepointIndex = 0;
            foreach (var group in allGroups)
            {
                // SQL Server limita el nombre de savepoint a 32 caracteres.
                var savepoint = $"grp{savepointIndex++}";
                await transaction.CreateSavepointAsync(savepoint);
                try
                {
                    await pipeline.BackfillAgentIfMissingAsync(group.TitularCustomerId, group.CurrentRowAgentUserId);
                    await PolicyPersistence.PersistAsync(
                        db, report, group.Policy, group.HistoryTransitions, group.Dependents,
                        group.SourceRowCount, group.SourceReferences, group.SourceFile,
                        group.CustomerName, group.CompanyName);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackToSavepointAsync(savepoint);
                    // El rollback deshace los INSERTs a nivel SQL, pero el change tracker
                    // de EF sigue creyendo que esas entidades (Policy con un Id que ya no
                    // existe, etc.) están "Unchanged" — sin este Clear(), el próximo grupo
                    // arrastra referencias a Ids fantasma y falla en cascada por FK.
                    db.ChangeTracker.Clear();
                    report.UnprocessableRows.Add(
                        $"{group.SourceFile} PolicyNumber={group.Policy.PolicyNumber} (refs: {string.Join(",", group.SourceReferences)}): {ex.InnerException?.Message ?? ex.Message}");
                }
            }

            await transaction.RollbackAsync();
            Console.WriteLine("\n[dry-run] Transacción revertida — no se persistió nada.");
        }

        // --commit: cada grupo de póliza (Customer/InsuranceCompany ya resueltos y
        // guardados aparte, son entidades reutilizables independientes de la Policy) se
        // persiste en su PROPIA transacción. Si un grupo falla, se hace rollback SOLO de
        // ese grupo — los anteriores ya committeados quedan intactos, y el chequeo de
        // idempotencia (PolicyNumber ya existente) permite reintentar el run entero más
        // tarde sin reprocesar lo que ya entró bien.
        private static async Task RunCommitAsync(AppDbContext db, CliOptions options, MigrationReport report)
        {
            var matcher = new EntityMatcher(db, report);
            await matcher.PreloadCachesAsync();
            var pipeline = new ImportPipeline(db, matcher, report, options.HistoryWindowDays);

            var allGroups = await PrepareAllFilesAsync(options, pipeline, report);

            foreach (var group in allGroups)
            {
                await using var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    await pipeline.BackfillAgentIfMissingAsync(group.TitularCustomerId, group.CurrentRowAgentUserId);
                    await PolicyPersistence.PersistAsync(
                        db, report, group.Policy, group.HistoryTransitions, group.Dependents,
                        group.SourceRowCount, group.SourceReferences, group.SourceFile,
                        group.CustomerName, group.CompanyName);
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    db.ChangeTracker.Clear();
                    report.UnprocessableRows.Add(
                        $"{group.SourceFile} PolicyNumber={group.Policy.PolicyNumber} (refs: {string.Join(",", group.SourceReferences)}): {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        private static async Task<List<PreparedPolicyGroup>> PrepareAllFilesAsync(
            CliOptions options, ImportPipeline pipeline, MigrationReport report)
        {
            var all = new List<PreparedPolicyGroup>();

            var healthFile = FindFile(options.SourceDir, "healthinsurance");
            var medicareFile = FindFile(options.SourceDir, "medicareinsurance");
            var lifeFile = FindFile(options.SourceDir, "lifeinsurance");
            var supplementalFile = FindFile(options.SourceDir, "supplementalinsurance");

            if (healthFile != null) all.AddRange(await HealthInsuranceImporter.RunAsync(healthFile, pipeline));
            if (medicareFile != null) all.AddRange(await MedicareImporter.RunAsync(medicareFile, pipeline));
            if (lifeFile != null) all.AddRange(await LifeInsuranceImporter.RunAsync(lifeFile, pipeline));
            if (supplementalFile != null) all.AddRange(await SupplementalImporter.RunAsync(supplementalFile, pipeline));

            return all;
        }

        private static string? FindFile(string sourceDir, string marker)
            => Directory.GetFiles(sourceDir, "*.xlsx")
                .FirstOrDefault(f => Path.GetFileName(f).Contains(marker, StringComparison.OrdinalIgnoreCase));

        private static string ResolveConnectionString(CliOptions options)
        {
            if (!string.IsNullOrEmpty(options.ConnectionString)) return options.ConnectionString;

            var apiDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "WholeCareInsurance.api");
            var config = new ConfigurationBuilder()
                .SetBasePath(Path.GetFullPath(apiDir))
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var conn = config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(conn))
                throw new InvalidOperationException(
                    "No se encontró ConnectionStrings:DefaultConnection en WholeCareInsurance.api/appsettings*.json. Usá --connection para pasarla explícitamente.");
            return conn;
        }
    }
}
