using Microsoft.EntityFrameworkCore;
using WholeCareInsurance.api.Data;
using WholeCareInsurance.Migration.Excel;

namespace WholeCareInsurance.Migration.Importers
{
    // §17.1: backfillea Phone y CreatedAt de los 41 agentes reales importados por
    // AgentImporter (§15.2) desde el mismo xlsx de origen — el export liviano de agentes
    // no traía estos 2 campos cuando se corrió esa importación, y son datos reales de
    // producción (no un default) que no se pueden perder. Cruza por Email, igual que la
    // idempotencia de AgentImporter — es la única columna del origen que es clave única
    // real (Nombre tiene colisiones conocidas, ver AgentImporter.NameCollisions).
    public static class AgentContactBackfillRunner
    {
        public static async Task<AgentContactBackfillReport> RunAsync(string filePath, AppDbContext db, bool commit)
        {
            var rows = ExcelReader.ReadRows(filePath);
            var report = new AgentContactBackfillReport();

            var usersByEmail = await db.Users.ToDictionaryAsync(u => u.Email, StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var email = row.GetString("Email")?.Trim();
                var phone = row.GetString("Phone");
                var registrationDate = row.GetDateTime("Registration date");
                var fullName = row.GetString("Full name") ?? "";

                if (string.IsNullOrEmpty(email))
                {
                    report.SkippedNoEmail.Add($"fila {row.RowNumber}: {fullName}");
                    continue;
                }

                if (!usersByEmail.TryGetValue(email, out var user))
                {
                    report.NotFound.Add($"{fullName} <{email}>: no existe ningún User con ese Email (¿todavía no se corrió el import de §15.2?).");
                    continue;
                }

                if (string.IsNullOrEmpty(phone) || registrationDate == null)
                {
                    report.MissingSourceData.Add($"{fullName} <{email}>: Phone='{phone ?? "(vacío)"}' RegistrationDate='{(registrationDate == null ? "(vacío)" : registrationDate.ToString())}'.");
                    continue;
                }

                report.Updated.Add($"{fullName} <{email}>: Phone '{user.Phone ?? "(vacío)"}' -> '{phone}', CreatedAt '{user.CreatedAt:yyyy-MM-dd HH:mm}' -> '{registrationDate:yyyy-MM-dd HH:mm}'");

                if (commit)
                {
                    user.Phone = phone;
                    user.CreatedAt = registrationDate.Value;
                }
            }

            if (commit && report.Updated.Count > 0)
                await db.SaveChangesAsync();

            return report;
        }
    }

    public class AgentContactBackfillReport
    {
        public List<string> Updated { get; } = new();
        public List<string> NotFound { get; } = new();
        public List<string> SkippedNoEmail { get; } = new();
        public List<string> MissingSourceData { get; } = new();

        public void Print(bool commit)
        {
            Console.WriteLine();
            Console.WriteLine("========== BACKFILL PHONE/CREATEDAT DE AGENTES (§17.1) ==========");
            Console.WriteLine($"Modo: {(commit ? "commit" : "dry-run")}");
            Console.WriteLine($"A actualizar: {Updated.Count}");
            foreach (var u in Updated) Console.WriteLine($"  {u}");
            if (NotFound.Count > 0)
            {
                Console.WriteLine($"Sin User existente con ese Email ({NotFound.Count}):");
                foreach (var n in NotFound) Console.WriteLine($"  {n}");
            }
            if (SkippedNoEmail.Count > 0)
            {
                Console.WriteLine($"Sin Email en el origen, salteados ({SkippedNoEmail.Count}):");
                foreach (var s in SkippedNoEmail) Console.WriteLine($"  {s}");
            }
            if (MissingSourceData.Count > 0)
            {
                Console.WriteLine($"Sin Phone y/o Registration date en el origen ({MissingSourceData.Count}):");
                foreach (var m in MissingSourceData) Console.WriteLine($"  {m}");
            }
            Console.WriteLine("===================================================================");
        }
    }
}
