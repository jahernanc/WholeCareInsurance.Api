using Microsoft.EntityFrameworkCore;
using WholeCareInsurance.api.Data;
using WholeCareInsurance.api.Models;
using WholeCareInsurance.Migration.Lookups;
using WholeCareInsurance.Migration.Reporting;

namespace WholeCareInsurance.Migration.Matching
{
    // Resuelve Customer/InsuranceCompany/Agente contra la base ya existente, creando
    // lo que haga falta. Cachea en memoria (write-through) porque el mismo run procesa
    // hasta 1258 filas y muchas se repiten (misma persona en varias filas de historial,
    // misma aseguradora en cientos de filas). El cache es seguro cruzando transacciones
    // por-Policy en modo commit (cada unidad ya se commiteó antes de que la próxima
    // consulte), y también dentro del savepoint-per-unit de dry-run (misma conexión).
    public class EntityMatcher
    {
        private readonly AppDbContext _db;
        private readonly MigrationReport _report;

        private readonly Dictionary<string, int> _companyByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _customerBySsn = new();
        private readonly Dictionary<string, List<int>> _customerByNameDob = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _agentByName = new(StringComparer.OrdinalIgnoreCase);
        private int? _fallbackAdminUserId;

        public EntityMatcher(AppDbContext db, MigrationReport report)
        {
            _db = db;
            _report = report;
        }

        public async Task PreloadCachesAsync()
        {
            foreach (var c in await _db.InsuranceCompanies.ToListAsync())
                _companyByName[c.Name.Trim()] = c.Id;

            foreach (var c in await _db.Customers.ToListAsync())
            {
                var ssn = NormalizeSsn(c.SocialSecurityNumber);
                if (ssn != null) _customerBySsn.TryAdd(ssn, c.Id);

                var key = NameDobKey(c.FirstName, c.LastName, c.DateOfBirth);
                if (!_customerByNameDob.TryGetValue(key, out var list))
                    _customerByNameDob[key] = list = new List<int>();
                list.Add(c.Id);
            }

            foreach (var u in await _db.Users.ToListAsync())
                _agentByName[u.Nombre.Trim()] = u.Id;
        }

        public static string? NormalizeSsn(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var trimmed = raw.Trim();
            if (EnumMaps.DummySsns.Contains(trimmed)) return null;
            return trimmed;
        }

        private static string NameDobKey(string firstName, string lastName, DateTime dob)
            => $"{firstName.Trim()}|{lastName.Trim()}|{dob:yyyy-MM-dd}".ToLowerInvariant();

        public async Task<int> ResolveInsuranceCompanyAsync(string rawName)
        {
            var name = rawName.Trim();
            if (_companyByName.TryGetValue(name, out var id)) return id;

            var existing = await _db.InsuranceCompanies
                .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
            if (existing != null)
            {
                _companyByName[name] = existing.Id;
                return existing.Id;
            }

            var created = new InsuranceCompany { Name = name, IsActive = true };
            _db.InsuranceCompanies.Add(created);
            await _db.SaveChangesAsync();
            _companyByName[name] = created.Id;
            _report.InsuranceCompaniesCreated.Add(name);
            return created.Id;
        }

        public async Task<int> ResolveAgentAsync(string sourceFile, int sourceRow, string? rawAgentName)
        {
            var name = rawAgentName?.Trim();
            if (!string.IsNullOrEmpty(name) && _agentByName.TryGetValue(name, out var id))
                return id;

            if (!string.IsNullOrEmpty(name))
            {
                var existing = await _db.Users.FirstOrDefaultAsync(u => u.Nombre.ToLower() == name.ToLower());
                if (existing != null)
                {
                    _agentByName[name] = existing.Id;
                    return existing.Id;
                }
            }

            _fallbackAdminUserId ??= (await _db.Users
                .Where(u => u.Rol == "Admin")
                .OrderBy(u => u.Id)
                .Select(u => u.Id)
                .FirstOrDefaultAsync());

            if (_fallbackAdminUserId is null or 0)
                throw new InvalidOperationException("No hay ningún User con Rol=Admin para usar como fallback de Agente.");

            _report.AgentFallbacks.Add(new AgentFallbackEntry
            {
                SourceFile = sourceFile,
                SourceRow = sourceRow,
                OriginalAgentName = name ?? "(vacío)",
            });
            return _fallbackAdminUserId.Value;
        }

        // Resultado del match de Customer: Id resuelto + si fue creado o matcheado,
        // para que el importer alimente las métricas del reporte.
        public async Task<CustomerMatchResult> ResolveCustomerAsync(CustomerSourceData data)
        {
            var ssn = NormalizeSsn(data.SocialSecurityNumber);
            // Se apaga si detectamos colisión (SSN ya usado por alguien con apellido
            // distinto): esa SSN no puede reutilizarse para un Customer nuevo tampoco,
            // así que la creación cae al placeholder NOSSN-<referencia>.
            var ssnUsableForCreate = ssn;

            if (ssn != null && _customerBySsn.TryGetValue(ssn, out var idBySsn))
            {
                var existing = await _db.Customers.FindAsync(idBySsn);
                if (existing != null && LastNamesLikelyMatch(existing.LastName, data.LastName))
                    return new CustomerMatchResult(idBySsn, CustomerMatchKind.MatchedBySsn);

                _report.SsnCollisionWarnings.Add(
                    $"SSN {ssn} ya pertenece a Customer #{idBySsn} (\"{existing?.LastName}\") pero la fila trae apellido \"{data.LastName}\" — no se fusiona, se intenta Nombre+Apellido+FechaNacimiento.");
                ssnUsableForCreate = null;
            }

            var nameDobKey = NameDobKey(data.FirstName, data.LastName, data.DateOfBirth);
            if (_customerByNameDob.TryGetValue(nameDobKey, out var candidates) && candidates.Count > 0)
                return new CustomerMatchResult(candidates[0], CustomerMatchKind.MatchedByNameDob);

            var customer = new Customer
            {
                SocialSecurityNumber = ssnUsableForCreate ?? BuildSsnPlaceholder(data.SourceReference),
                FirstName = Truncate(data.FirstName, 100, data.SourceReference, "FirstName"),
                LastName = Truncate(data.LastName, 100, data.SourceReference, "LastName"),
                DateOfBirth = data.DateOfBirth,
                Email = await ResolveUniqueEmailAsync(data.Email, data.SourceReference),
                Address1 = Truncate(data.Address1, 300, data.SourceReference, "Address1") ?? "",
                Phone = Truncate(data.Phone, 20, data.SourceReference, "Phone") ?? "",
                MigrationStatus = MapOrDefault(EnumMaps.MigrationStatus, data.LegalStatus, "Otro"),
                RelacionConPrincipal = data.RelacionConPrincipal,
                ZipCode = Truncate(data.ZipCode, 10, data.SourceReference, "ZipCode"),
                State = UsStates.ToCode(data.State),
                City = Truncate(data.City, 100, data.SourceReference, "City"),
                County = Truncate(data.County, 100, data.SourceReference, "County"),
                MaritalStatus = data.MaritalStatus != null && EnumMaps.MaritalStatus.TryGetValue(data.MaritalStatus, out var ms) ? ms : null,
                Occupation = Truncate(data.Occupation, 100, data.SourceReference, "Occupation"),
                MiddleName = Truncate(data.MiddleName, 100, data.SourceReference, "MiddleName"),
                Gender = data.Gender != null && EnumMaps.Gender.TryGetValue(data.Gender, out var g) ? g : null,
                GreenCard = Truncate(data.GreenCard, 50, data.SourceReference, "GreenCard"),
                WorkPermit = Truncate(data.WorkPermit, 50, data.SourceReference, "WorkPermit"),
                Address2 = Truncate(data.Address2, 300, data.SourceReference, "Address2"),
                EmployerName = Truncate(data.EmployerName, 150, data.SourceReference, "EmployerName"),
                CompanyPhone = Truncate(data.CompanyPhone, 20, data.SourceReference, "CompanyPhone"),
                AnnualIncome = data.AnnualIncome ?? 0,
            };

            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();

            if (ssnUsableForCreate != null) _customerBySsn[ssnUsableForCreate] = customer.Id;
            if (!_customerByNameDob.TryGetValue(nameDobKey, out var list))
                _customerByNameDob[nameDobKey] = list = new List<int>();
            list.Add(customer.Id);

            return new CustomerMatchResult(customer.Id, CustomerMatchKind.Created);
        }

        // Chequeo laxo (substring en cualquier dirección) para no descartar un match
        // legítimo por variantes de formato ("huycy" vs "huycy reyes") pero sí cortar
        // colisiones de SSN dummy entre personas sin ninguna relación textual.
        // Customer.SocialSecurityNumber tiene MaxLength(20) — "NS-" + hasta 17
        // caracteres de la referencia de origen entra siempre (Reference observado:
        // 16 caracteres, ej. "P15072026018434").
        private static string BuildSsnPlaceholder(string sourceReference)
        {
            var alnum = new string(sourceReference.Where(char.IsLetterOrDigit).ToArray());
            var tail = alnum.Length > 17 ? alnum[^17..] : alnum;
            return $"NS-{tail}";
        }

        // Varios campos de texto libre del origen (Green card, Work permit, etc.)
        // exceden el MaxLength de la columna en casos reales (ej. "USCIS 206874909
        // 07-19-2026 RECIBO NUMERO: MSC1690902769" > 50 chars) — se trunca en vez de
        // dejar que el INSERT explote, y se deja constancia en el reporte.
        private string? Truncate(string? value, int maxLength, string sourceReference, string fieldName)
        {
            if (value == null || value.Length <= maxLength) return value;
            _report.MissingDataWarnings.Add(
                $"Fila {sourceReference}: \"{fieldName}\" truncado de {value.Length} a {maxLength} caracteres (\"{value}\").");
            return value[..maxLength];
        }

        private static bool LastNamesLikelyMatch(string a, string b)
        {
            var x = a.Trim().ToLowerInvariant();
            var y = b.Trim().ToLowerInvariant();
            if (x.Length == 0 || y.Length == 0) return false;
            return x.Contains(y) || y.Contains(x);
        }

        private async Task<string> ResolveUniqueEmailAsync(string? rawEmail, string sourceReference)
        {
            var candidate = string.IsNullOrWhiteSpace(rawEmail)
                ? $"noemail+{sourceReference}@migracion.wholecare.local"
                : rawEmail.Trim();

            var taken = await _db.Customers.AnyAsync(c => c.Email.ToLower() == candidate.ToLower());
            if (!taken) return candidate;

            _report.MissingDataWarnings.Add(
                $"Email \"{candidate}\" ya está en uso por otro Customer (fila origen {sourceReference}) — se ajustó agregando sufijo para no violar el índice único.");

            var at = candidate.IndexOf('@');
            return at > 0
                ? $"{candidate[..at]}+mig{sourceReference}{candidate[at..]}"
                : $"{candidate}+mig{sourceReference}@migracion.wholecare.local";
        }

        private static string MapOrDefault(Dictionary<string, string> map, string? key, string fallback)
            => key != null && map.TryGetValue(key, out var v) ? v : fallback;
    }

    public enum CustomerMatchKind { MatchedBySsn, MatchedByNameDob, Created }

    public record CustomerMatchResult(int CustomerId, CustomerMatchKind Kind);

    public class CustomerSourceData
    {
        public string SourceReference { get; set; } = default!;
        public string? SocialSecurityNumber { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public DateTime DateOfBirth { get; set; }
        public string? Email { get; set; }
        public string? Address1 { get; set; }
        public string? Phone { get; set; }
        public string? LegalStatus { get; set; }
        public string RelacionConPrincipal { get; set; } = EnumMaps.TitularRelacionConPrincipal;
        public string? ZipCode { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? County { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Occupation { get; set; }
        public string? MiddleName { get; set; }
        public string? Gender { get; set; }
        public string? GreenCard { get; set; }
        public string? WorkPermit { get; set; }
        public string? Address2 { get; set; }
        public string? EmployerName { get; set; }
        public string? CompanyPhone { get; set; }
        public decimal? AnnualIncome { get; set; }
    }
}
