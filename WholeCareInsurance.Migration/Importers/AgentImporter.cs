using Microsoft.EntityFrameworkCore;
using WholeCareInsurance.api.Data;
using WholeCareInsurance.api.Models;
using WholeCareInsurance.Migration.Excel;

namespace WholeCareInsurance.Migration.Importers
{
    // §15.2: importa los 41 agentes reales del sistema anterior como User. A diferencia
    // de los 4 importers de pólizas, no hay Customer/Policy involucrados — es un mapeo
    // directo fila -> User, sin ImportPipeline/EntityMatcher.
    public static class AgentImporter
    {
        public const string SourceFileLabel = "Agentes";

        // Confirmado con el responsable (§15.2): estos 2 emails del archivo real van con
        // Rol=Admin (Alejandra Díaz Cortez / Alexander Centeno); el resto (39 filas) -> Agente.
        private static readonly HashSet<string> AdminEmails = new(StringComparer.OrdinalIgnoreCase)
        {
            "admin@wholecareinsurancellc.com",
            "alexfinancial22@gmail.com",
        };

        public static async Task<AgentImportReport> RunAsync(string filePath, AppDbContext db, bool commit, string sharedTempPassword)
        {
            var rows = ExcelReader.ReadRows(filePath);
            var report = new AgentImportReport();

            // Idempotencia por Email (§15.2): un re-run no debe duplicar agentes ya importados.
            var existingEmails = new HashSet<string>(await db.Users.Select(u => u.Email).ToListAsync(), StringComparer.OrdinalIgnoreCase);
            var existingNames = new HashSet<string>(await db.Users.Select(u => u.Nombre).ToListAsync(), StringComparer.OrdinalIgnoreCase);

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(sharedTempPassword);

            foreach (var row in rows)
            {
                var firstName = row.GetString("First name") ?? "";
                var lastName = row.GetString("Last name") ?? "";
                var nombre = $"{firstName} {lastName}".Trim();
                var email = row.GetString("Email")?.Trim();
                var agency = row.GetString("Agency");

                if (string.IsNullOrEmpty(email))
                {
                    report.SkippedNoEmail.Add($"fila {row.RowNumber}: {nombre}");
                    continue;
                }

                if (existingEmails.Contains(email))
                {
                    report.SkippedAlreadyExists.Add($"{nombre} <{email}>");
                    continue;
                }

                if (existingNames.Contains(nombre))
                    report.NameCollisions.Add($"{nombre} <{email}> — ya existe un User con el mismo Nombre y otro Email, revisar a mano.");

                var rol = AdminEmails.Contains(email) ? "Admin" : "Agente";

                var user = new User
                {
                    Nombre = nombre,
                    Email = email,
                    PasswordHash = passwordHash,
                    Rol = rol,
                    IsEncargado = false,
                    Agency = agency,
                    // Placeholder vacío, a completar manualmente después (mismo criterio que el backfill de §11.2) —
                    // el export liviano de agentes no trae dirección/licencia/NPN.
                    Address1 = "",
                    City = "",
                    ZipCode = "",
                    State = "",
                    County = "",
                    Licensed = false,
                    HasCompanyContract = false,
                    NpnOverride = false,
                    MustChangePassword = true,
                    // Decisión confirmada: no hay evidencia de que hayan aceptado ESTOS términos nuevos.
                    TermsAccepted = false,
                    TermsAcceptedAt = null,
                };

                if (commit)
                    db.Users.Add(user);

                report.Created.Add($"{nombre} <{email}> (Rol={rol}, Agency={agency ?? "(vacío)"})");
                existingEmails.Add(email);
                existingNames.Add(nombre);
            }

            if (commit && report.Created.Count > 0)
                await db.SaveChangesAsync();

            return report;
        }
    }

    public class AgentImportReport
    {
        public List<string> Created { get; } = new();
        public List<string> SkippedAlreadyExists { get; } = new();
        public List<string> SkippedNoEmail { get; } = new();
        public List<string> NameCollisions { get; } = new();

        public void Print(bool commit, string? sharedTempPassword)
        {
            Console.WriteLine();
            Console.WriteLine("========== IMPORTACIÓN DE AGENTES (§15.2) ==========");
            Console.WriteLine($"Modo: {(commit ? "commit" : "dry-run")}");
            Console.WriteLine($"A crear: {Created.Count}");
            foreach (var c in Created) Console.WriteLine($"  {c}");
            Console.WriteLine($"Ya existían (salteados por Email): {SkippedAlreadyExists.Count}");
            foreach (var s in SkippedAlreadyExists) Console.WriteLine($"  {s}");
            if (SkippedNoEmail.Count > 0)
            {
                Console.WriteLine($"Sin Email en el origen, salteados ({SkippedNoEmail.Count}):");
                foreach (var s in SkippedNoEmail) Console.WriteLine($"  {s}");
            }
            if (NameCollisions.Count > 0)
            {
                Console.WriteLine($"Colisiones de Nombre a revisar a mano ({NameCollisions.Count}):");
                foreach (var n in NameCollisions) Console.WriteLine($"  {n}");
            }
            if (Created.Count > 0 && sharedTempPassword != null)
            {
                Console.WriteLine();
                Console.WriteLine($"Password temporal {(commit ? "asignada" : "que se asignaría")} a los {Created.Count} agentes nuevos: {sharedTempPassword}");
                Console.WriteLine("(comunicar manualmente — no queda en ningún archivo versionado en git; MustChangePassword=true fuerza el cambio en el primer login).");
            }
            Console.WriteLine("=====================================================");
        }
    }
}
