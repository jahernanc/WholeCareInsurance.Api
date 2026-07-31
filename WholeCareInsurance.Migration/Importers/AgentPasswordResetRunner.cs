using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using WholeCareInsurance.api.Data;
using WholeCareInsurance.Migration.Excel;

namespace WholeCareInsurance.Migration.Importers
{
    // Corrige la contraseña ÚNICA compartida que AgentImporter (§15.2) asignó a los 41
    // agentes reales: acá se genera una contraseña individual por agente (mismo mecanismo,
    // RandomNumberGenerator, pero una llamada por fila en vez de una sola para todos).
    // Re-lee el mismo xlsx de agentes y matchea por Email, igual que AgentContactBackfillRunner.
    public static class AgentPasswordResetRunner
    {
        // Casos confirmados donde el email del User real diverge del xlsx original de §15.2
        // porque el agente usó el cambio de email autoservicio de Profile.jsx (§26) después
        // del import — el xlsx de origen nunca se actualiza, así que el match por Email solo
        // no alcanza. Confirmado con SQL directo antes de agregar cada entrada.
        private static readonly Dictionary<string, int> KnownEmailDriftToUserId = new(StringComparer.OrdinalIgnoreCase)
        {
            // Alexander Centeno: xlsx trae alexfinancial22@gmail.com; email actual (correcto,
            // cambio intencional) es info@wholecareinsurancellc.com, User.Id=3011.
            ["alexfinancial22@gmail.com"] = 3011,
        };

        public static async Task<AgentPasswordResetReport> RunAsync(string filePath, AppDbContext db, bool commit)
        {
            var rows = ExcelReader.ReadRows(filePath);
            var report = new AgentPasswordResetReport();

            var usersByEmail = await db.Users.ToDictionaryAsync(u => u.Email, StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var firstName = row.GetString("First name") ?? "";
                var lastName = row.GetString("Last name") ?? "";
                var nombre = $"{firstName} {lastName}".Trim();
                var email = row.GetString("Email")?.Trim();

                if (string.IsNullOrEmpty(email))
                {
                    report.SkippedNoEmail.Add($"fila {row.RowNumber}: {nombre}");
                    continue;
                }

                if (!usersByEmail.TryGetValue(email, out var user))
                {
                    if (KnownEmailDriftToUserId.TryGetValue(email, out var knownId))
                        user = await db.Users.FindAsync(knownId);

                    if (user == null)
                    {
                        report.NotFound.Add($"{nombre} <{email}>: no existe ningún User con ese Email (¿todavía no se corrió el import de §15.2?).");
                        continue;
                    }

                    report.MatchedByKnownIdOverride.Add($"{nombre} <{email} del xlsx> -> User.Id={user.Id} (email actual: {user.Email})");
                }

                var tempPassword = GenerateTempPassword();
                report.Updated.Add(new AgentPasswordResetEntry(nombre, user.Email, tempPassword));

                if (commit)
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
                    user.MustChangePassword = true;
                }
            }

            if (commit && report.Updated.Count > 0)
                await db.SaveChangesAsync();

            return report;
        }

        // Mismo criterio que GenerateTempPassword() de Program.cs (§15.2) y los
        // refresh/reset tokens de AuthService (§10.4): RandomNumberGenerator, no Guid.NewGuid().
        // A diferencia de Program.cs, acá se llama una vez POR AGENTE, no una sola vez compartida.
        private static string GenerateTempPassword()
            => Convert.ToBase64String(RandomNumberGenerator.GetBytes(12)).Replace("+", "9").Replace("/", "8").Replace("=", "");
    }

    public record AgentPasswordResetEntry(string Nombre, string Email, string TempPassword);

    public class AgentPasswordResetReport
    {
        public List<AgentPasswordResetEntry> Updated { get; } = new();
        public List<string> NotFound { get; } = new();
        public List<string> SkippedNoEmail { get; } = new();
        public List<string> MatchedByKnownIdOverride { get; } = new();

        public void Print(bool commit, string? passwordsFilePath)
        {
            Console.WriteLine();
            Console.WriteLine("========== RESET DE CONTRASEÑAS INDIVIDUALES DE AGENTES ==========");
            Console.WriteLine($"Modo: {(commit ? "commit" : "dry-run")}");
            Console.WriteLine($"A actualizar: {Updated.Count}");
            foreach (var u in Updated)
                Console.WriteLine($"  {u.Nombre} <{u.Email}>: {u.TempPassword}");
            if (MatchedByKnownIdOverride.Count > 0)
            {
                Console.WriteLine($"Matcheados por override manual de Id, no por Email del xlsx ({MatchedByKnownIdOverride.Count}):");
                foreach (var m in MatchedByKnownIdOverride) Console.WriteLine($"  {m}");
            }
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
            if (Updated.Count > 0 && passwordsFilePath != null)
            {
                Console.WriteLine();
                Console.WriteLine($"Contraseñas {(commit ? "asignadas" : "que se asignarían")} guardadas en: {passwordsFilePath}");
                Console.WriteLine("(fuera del repo git — mismo criterio que los backups de D:\\backups — para envío manual o para preparar el email de bienvenida cuando el VPS esté desplegado).");
            }
            Console.WriteLine("====================================================================");
        }

        public void SavePasswordsFile(string path, bool commit)
        {
            using var writer = new StreamWriter(path, append: false);
            writer.WriteLine($"# Contraseñas temporales individuales de agentes — {(commit ? "COMMIT" : "DRY-RUN, no persistido en la base")}");
            writer.WriteLine($"# Generado: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine("# Nombre | Email | Contraseña temporal");
            foreach (var u in Updated)
                writer.WriteLine($"{u.Nombre} | {u.Email} | {u.TempPassword}");
        }
    }
}
