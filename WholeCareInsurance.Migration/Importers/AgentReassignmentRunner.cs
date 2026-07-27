using Microsoft.EntityFrameworkCore;
using WholeCareInsurance.api.Data;
using WholeCareInsurance.Migration.Excel;
using WholeCareInsurance.Migration.Matching;
using WholeCareInsurance.Migration.Reporting;

namespace WholeCareInsurance.Migration.Importers
{
    // §15.3: reasigna Customer.AgentId en las filas que quedaron con el fallback Admin
    // durante la migración original (§7), ahora que los 41 agentes reales ya existen
    // como User (§15.2). Relee los 4 xlsx de pólizas (nunca los modifica) y reutiliza
    // EntityMatcher para re-resolver Customer/Agente con el mismo criterio determinístico
    // que usó la migración original — pero en modo solo-lectura (TryFindExisting*, nunca crea).
    //
    // Simplificación consciente frente a ImportPipeline/PolicyGrouper: acá no hace falta
    // reconstruir la identidad exacta de cada Policy (agrupar por PolicyNumber/aseguradora/
    // ventana de historial) — AgentId vive en Customer, no en Policy, así que alcanza con
    // encontrar, para cada Customer, la fila con el "Update date" más reciente entre TODAS
    // sus apariciones en los 4 archivos, y usar el Agente de esa fila. Es el mismo criterio
    // de "más reciente gana" que ya usaba PolicyGrouper.CurrentOf, aplicado a nivel Customer
    // en vez de a nivel Policy (que es el nivel correcto para este campo).
    public static class AgentReassignmentRunner
    {
        public static async Task<AgentReassignmentReport> RunAsync(string sourceDir, AppDbContext db, bool commit)
        {
            var report = new AgentReassignmentReport();
            var matcher = new EntityMatcher(db, new MigrationReport());
            await matcher.PreloadCachesAsync();

            var fallbackAdminId = await db.Users
                .Where(u => u.Rol == "Admin")
                .OrderBy(u => u.Id)
                .Select(u => u.Id)
                .FirstAsync();

            var files = new (string? Path, string Label)[]
            {
                (FindFile(sourceDir, "healthinsurance"), "Health/Obamacare"),
                (FindFile(sourceDir, "medicareinsurance"), "Medicare"),
                (FindFile(sourceDir, "lifeinsurance"), "Life Insurance"),
                (FindFile(sourceDir, "supplementalinsurance"), "Supplemental Plans"),
            };

            // Customer.Id -> (Update date más reciente visto, Agente de esa fila).
            var latestByCustomer = new Dictionary<int, (DateTime UpdateDate, string AgentName)>();
            var scratchReport = new MigrationReport();

            foreach (var (path, label) in files)
            {
                if (path == null) continue;

                foreach (var row in ExcelReader.ReadRows(path))
                {
                    var fields = CommonFieldsExtractor.ExtractPolicyFields(row, scratchReport, label);
                    var customerData = CommonFieldsExtractor.ExtractCustomer(row, fields.Reference);
                    var customerId = matcher.TryFindExistingCustomerId(customerData);

                    if (customerId == null)
                    {
                        report.UnmatchedRows.Add($"{label} fila {row.RowNumber} (Ref {fields.Reference}): sin match de Customer, no se pudo evaluar.");
                        continue;
                    }

                    if (!latestByCustomer.TryGetValue(customerId.Value, out var current) || fields.UpdateDate > current.UpdateDate)
                        latestByCustomer[customerId.Value] = (fields.UpdateDate, fields.AgentNameRaw);
                }
            }

            foreach (var (customerId, data) in latestByCustomer)
            {
                var customer = await db.Customers.FindAsync(customerId);
                if (customer == null) continue;

                // Salvaguarda explícita: no pisar una reasignación manual ya hecha
                // (o un Customer que ya tenía agente real desde el principio).
                if (customer.AgentId != fallbackAdminId)
                {
                    report.SkippedNotFallback.Add($"Customer #{customerId}: AgentId ya no es el fallback ({customer.AgentId}), no se toca.");
                    continue;
                }

                var realAgentId = matcher.TryFindExistingAgentId(data.AgentName);
                if (realAgentId == null)
                {
                    report.StillFallback.Add($"Customer #{customerId}: agente original \"{(string.IsNullOrEmpty(data.AgentName) ? "(vacío)" : data.AgentName)}\" no matchea ningún User real, sigue en fallback.");
                    continue;
                }

                report.Reassigned.Add($"Customer #{customerId}: {fallbackAdminId} -> {realAgentId} (\"{data.AgentName}\")");
                if (commit)
                    customer.AgentId = realAgentId;
            }

            if (commit && report.Reassigned.Count > 0)
                await db.SaveChangesAsync();

            return report;
        }

        private static string? FindFile(string sourceDir, string marker)
            => Directory.GetFiles(sourceDir, "*.xlsx")
                .FirstOrDefault(f => Path.GetFileName(f).Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    public class AgentReassignmentReport
    {
        public List<string> Reassigned { get; } = new();
        public List<string> StillFallback { get; } = new();
        public List<string> SkippedNotFallback { get; } = new();
        public List<string> UnmatchedRows { get; } = new();

        public void Print(bool commit)
        {
            Console.WriteLine();
            Console.WriteLine("========== REASIGNACIÓN DE AGENTES (§15.3) ==========");
            Console.WriteLine($"Modo: {(commit ? "commit" : "dry-run")}");
            Console.WriteLine($"Reasignados: {Reassigned.Count}");
            foreach (var r in Reassigned) Console.WriteLine($"  {r}");
            Console.WriteLine($"Siguen en fallback (agente original no matchea ningún User real): {StillFallback.Count}");
            foreach (var s in StillFallback.Take(30)) Console.WriteLine($"  {s}");
            if (StillFallback.Count > 30) Console.WriteLine($"  ... y {StillFallback.Count - 30} más");
            if (SkippedNotFallback.Count > 0)
            {
                Console.WriteLine($"Salteados (ya no estaban en fallback, no se tocaron): {SkippedNotFallback.Count}");
                foreach (var s in SkippedNotFallback.Take(10)) Console.WriteLine($"  {s}");
                if (SkippedNotFallback.Count > 10) Console.WriteLine($"  ... y {SkippedNotFallback.Count - 10} más");
            }
            if (UnmatchedRows.Count > 0)
            {
                Console.WriteLine($"Filas del origen sin match de Customer ({UnmatchedRows.Count}):");
                foreach (var u in UnmatchedRows.Take(10)) Console.WriteLine($"  {u}");
                if (UnmatchedRows.Count > 10) Console.WriteLine($"  ... y {UnmatchedRows.Count - 10} más");
            }
            Console.WriteLine("======================================================");
        }
    }
}
