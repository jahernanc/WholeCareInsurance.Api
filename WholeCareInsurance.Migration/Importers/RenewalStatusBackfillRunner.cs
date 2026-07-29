using Microsoft.EntityFrameworkCore;
using WholeCareInsurance.api.Data;
using WholeCareInsurance.Migration.Excel;
using WholeCareInsurance.Migration.Matching;
using WholeCareInsurance.Migration.Reporting;

namespace WholeCareInsurance.Migration.Importers
{
    // §18.1/§18.6 paso 2: backfillea Policy.RenewalStatus re-leyendo el xlsx real de
    // Health/Obamacare (única fuente — Medicare/Life/Supplemental no tienen esta columna).
    //
    // No matchea por el texto crudo de "Policy number" contra Policy.PolicyNumber: ese
    // valor viene vacío/basura en el 90%+ de las filas (ver PolicyNumberResolver), y el
    // PolicyNumber real que terminó en la base para esas filas es el "Reference" de la
    // fila VIGENTE de cada grupo consolidado de historial (ImportPipeline.cs:142-143), no
    // el de cualquier fila. En vez de reimplementar esa heurística de consolidación acá
    // (repetible, pero frágil si diverge un byte del pipeline real), se reutiliza el
    // mismo HealthInsuranceImporter/ImportPipeline que ya migró los datos: se le pide que
    // vuelva a preparar los mismos grupos (misma resolución de Customer/Aseguradora,
    // dentro de una transacción que SIEMPRE se revierte, nunca escribe nada por sí sola)
    // y se toma el PolicyNumber + RenewalStatus ya resueltos de la fila vigente de cada
    // grupo — así el match contra la Policy real en la base es por construcción exacto,
    // sin heurística propia.
    public static class RenewalStatusBackfillRunner
    {
        public static async Task<RenewalStatusBackfillReport> RunAsync(string filePath, AppDbContext db, bool commit)
        {
            var report = new RenewalStatusBackfillReport();

            List<PreparedPolicyGroup> groups;
            await using (var transaction = await db.Database.BeginTransactionAsync())
            {
                var scratchReport = new MigrationReport();
                var matcher = new EntityMatcher(db, scratchReport);
                await matcher.PreloadCachesAsync();
                var pipeline = new ImportPipeline(db, matcher, scratchReport, historyWindowDays: 200);

                groups = await HealthInsuranceImporter.RunAsync(filePath, pipeline);

                // Nunca se persiste nada de la re-preparación (ni Customers/Companies que
                // el matcher pudiera tocar) — solo interesan los objetos en memoria.
                await transaction.RollbackAsync();
                db.ChangeTracker.Clear();
            }

            var policyNumbers = groups.Select(g => g.Policy.PolicyNumber).ToList();
            var existingByNumber = await db.Policies
                .Where(p => policyNumbers.Contains(p.PolicyNumber))
                .ToDictionaryAsync(p => p.PolicyNumber);

            foreach (var group in groups)
            {
                var policyNumber = group.Policy.PolicyNumber;
                var renewalStatus = group.Policy.RenewalStatus;

                if (!existingByNumber.TryGetValue(policyNumber, out var existing))
                {
                    report.NotFoundInDb.Add($"PolicyNumber={policyNumber} ({group.CustomerName}): no existe ninguna Policy con ese número en la base.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(renewalStatus))
                {
                    report.SourceEmpty.Add($"PolicyNumber={policyNumber} ({group.CustomerName}): fila vigente sin \"Renewal status\" en el xlsx.");
                    continue;
                }

                report.ToUpdate.Add($"PolicyNumber={policyNumber} ({group.CustomerName}): '{existing.RenewalStatus ?? "(vacío)"}' -> '{renewalStatus}'");

                if (commit)
                    existing.RenewalStatus = renewalStatus;
            }

            report.TotalHealthPolicies = groups.Count;

            if (commit && report.ToUpdate.Count > 0)
                await db.SaveChangesAsync();

            return report;
        }
    }

    public class RenewalStatusBackfillReport
    {
        public int TotalHealthPolicies { get; set; }
        public List<string> ToUpdate { get; } = new();
        public List<string> SourceEmpty { get; } = new();
        public List<string> NotFoundInDb { get; } = new();

        public void Print(bool commit)
        {
            Console.WriteLine();
            Console.WriteLine("========== BACKFILL RENEWAL STATUS (§18.6 paso 2) ==========");
            Console.WriteLine($"Modo: {(commit ? "commit" : "dry-run")}");
            Console.WriteLine($"Pólizas de Health/Obamacare re-preparadas: {TotalHealthPolicies}");
            Console.WriteLine($"Matchean y tienen RenewalStatus para actualizar: {ToUpdate.Count}");
            Console.WriteLine($"Matchean pero sin dato en el origen (\"Renewal status\" vacío): {SourceEmpty.Count}");
            Console.WriteLine($"No se encontró Policy en la base con ese PolicyNumber: {NotFoundInDb.Count}");
            Console.WriteLine();

            Console.WriteLine($"--- A actualizar ({ToUpdate.Count}) ---");
            foreach (var u in ToUpdate) Console.WriteLine($"  {u}");

            if (SourceEmpty.Count > 0)
            {
                Console.WriteLine($"--- Sin dato en el origen ({SourceEmpty.Count}) ---");
                foreach (var s in SourceEmpty) Console.WriteLine($"  {s}");
            }

            if (NotFoundInDb.Count > 0)
            {
                Console.WriteLine($"--- Sin match en la base ({NotFoundInDb.Count}) ---");
                foreach (var n in NotFoundInDb) Console.WriteLine($"  {n}");
            }

            Console.WriteLine("================================================================");
        }
    }
}
