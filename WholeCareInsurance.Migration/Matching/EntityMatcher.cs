using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
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
        // §23.3: candidatos a "posible duplicado" agrupados por DOB exacto — solo se
        // consulta cuando el NameDobKey exacto de arriba ya falló. Nunca decide un match,
        // solo alimenta CheckPossibleDuplicate para dejar constancia en el reporte.
        private readonly Dictionary<DateTime, List<CustomerFuzzyRecord>> _customerByDob = new();
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

                AddToDobCache(c.Id, c.FirstName, c.LastName, c.Phone, c.Address1, c.DateOfBirth);
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
        // §15.3: variante de solo lectura para la reasignación de AgentId — NUNCA crea un
        // Customer nuevo (a diferencia de ResolveCustomerAsync). Todas las personas de los
        // 4 archivos ya deberían existir de la migración original; si una fila no matchea
        // acá, se reporta como no evaluable en vez de crear un Customer fantasma.
        public int? TryFindExistingCustomerId(CustomerSourceData data)
        {
            var ssn = NormalizeSsn(data.SocialSecurityNumber);
            if (ssn != null && _customerBySsn.TryGetValue(ssn, out var idBySsn))
                return idBySsn;

            var nameDobKey = NameDobKey(data.FirstName, data.LastName, data.DateOfBirth);
            if (_customerByNameDob.TryGetValue(nameDobKey, out var candidates) && candidates.Count > 0)
                return candidates[0];

            return null;
        }

        // §15.3: variante de solo lectura para Agente — a diferencia de ResolveAgentAsync,
        // NUNCA cae a un fallback: devuelve null si el nombre no matchea ningún User real.
        public int? TryFindExistingAgentId(string? rawAgentName)
        {
            var name = rawAgentName?.Trim();
            if (string.IsNullOrEmpty(name)) return null;
            return _agentByName.TryGetValue(name, out var id) ? id : null;
        }

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

            CheckPossibleDuplicate(data);

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
            AddToDobCache(customer.Id, customer.FirstName!, customer.LastName!, customer.Phone, customer.Address1, customer.DateOfBirth);

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

        private void AddToDobCache(int id, string firstName, string lastName, string? phone, string? address1, DateTime dob)
        {
            var record = new CustomerFuzzyRecord(id, NormalizeNamePart(firstName), NormalizeNamePart(lastName), NormalizePhone(phone), NormalizeAddress(address1));
            var dobDate = dob.Date;
            if (!_customerByDob.TryGetValue(dobDate, out var list))
                _customerByDob[dobDate] = list = new List<CustomerFuzzyRecord>();
            list.Add(record);
        }

        // §23.3: reporta (NUNCA fusiona ni descarta) posibles duplicados de Customer que
        // el NameDobKey exacto no detecta por variaciones de formato de apellido — ver
        // caso real Doris Maldonado / Maldonado Ramirez, PENDIENTE.md §23.2. Solo dispara
        // cuando: mismo DOB (agrupado en _customerByDob), FirstName normalizado idéntico
        // (sin tolerancia — evita falsos positivos con nombres comunes), LastName
        // "variante" del mismo apellido (uno contiene al otro tras sacar espacios/guiones,
        // o distancia de edición ≤2 — cubre apellido compuesto y typos de formato) Y,
        // además, Phone o Address1 normalizados coinciden exactamente. Esta última señal
        // es la que separa "misma persona" de la simple coincidencia de cumpleaños: en
        // los 2,130 Customers reales hay 96 fechas de nacimiento compartidas por más de
        // una persona, casi todas sin relación — exigir Phone o Address además del
        // apellido variante es lo que evitó falsos positivos en el análisis manual previo.
        // La creación del Customer sigue igual que antes de este cambio: esto solo deja
        // constancia en el reporte para revisión humana, igual que SsnCollisionWarnings.
        private void CheckPossibleDuplicate(CustomerSourceData data)
        {
            if (!_customerByDob.TryGetValue(data.DateOfBirth.Date, out var candidates))
                return;

            var firstNameNorm = NormalizeNamePart(data.FirstName);
            var lastNameNorm = NormalizeNamePart(data.LastName);
            var phoneNorm = NormalizePhone(data.Phone);
            var addressNorm = NormalizeAddress(data.Address1);

            foreach (var candidate in candidates)
            {
                if (candidate.FirstNameNorm != firstNameNorm) continue;
                if (candidate.LastNameNorm == lastNameNorm) continue; // esto ya lo habría atrapado el NameDobKey exacto

                var lastNameVariant = lastNameNorm.Length > 0 && candidate.LastNameNorm.Length > 0
                    && (lastNameNorm.Contains(candidate.LastNameNorm) || candidate.LastNameNorm.Contains(lastNameNorm)
                        || Levenshtein(lastNameNorm, candidate.LastNameNorm) <= 2);
                if (!lastNameVariant) continue;

                var phoneMatch = phoneNorm.Length > 0 && phoneNorm == candidate.PhoneNorm;
                var addressMatch = addressNorm.Length > 0 && addressNorm == candidate.AddressNorm;
                if (!phoneMatch && !addressMatch) continue;

                _report.PossibleDuplicateWarnings.Add(
                    $"Fila {data.SourceReference} (\"{data.FirstName} {data.LastName}\", DOB {data.DateOfBirth:yyyy-MM-dd}): posible duplicado de Customer #{candidate.Id} — mismo nombre y fecha de nacimiento, apellido con formato distinto, coincide {(phoneMatch ? "Phone" : "Address1")}. Se creó un Customer nuevo igual (no se fusiona automáticamente) — revisar manualmente.");
            }
        }

        private static string StripDiacritics(string s)
        {
            var normalized = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string NormalizeNamePart(string s)
            => new(StripDiacritics(s.Trim().ToLowerInvariant()).Where(char.IsLetterOrDigit).ToArray());

        private static string NormalizePhone(string? s)
            => new((s ?? "").Where(char.IsDigit).ToArray());

        private static readonly (string Pattern, string Replacement)[] AddressAbbreviations =
        {
            (@"\bdrive\b", "dr"), (@"\bstreet\b", "st"), (@"\bcourt\b", "ct"),
            (@"\bavenue\b", "ave"), (@"\bapartment\b", "apt"), (@"\bboulevard\b", "blvd"),
            (@"\blane\b", "ln"), (@"\bcircle\b", "cir"), (@"\bplace\b", "pl"),
            (@"\bnorth\b", "n"), (@"\bsouth\b", "s"), (@"\beast\b", "e"), (@"\bwest\b", "w"),
        };

        private static string NormalizeAddress(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var v = StripDiacritics(s.Trim().ToLowerInvariant());
            v = Regex.Replace(v, "[.,#]", " ");
            foreach (var (pattern, replacement) in AddressAbbreviations)
                v = Regex.Replace(v, pattern, replacement);
            return Regex.Replace(v, @"\s+", " ").Trim();
        }

        private static int Levenshtein(string a, string b)
        {
            if (a == b) return 0;
            if (a.Length == 0) return b.Length;
            if (b.Length == 0) return a.Length;

            var prev = new int[b.Length + 1];
            var cur = new int[b.Length + 1];
            for (var j = 0; j <= b.Length; j++) prev[j] = j;

            for (var i = 1; i <= a.Length; i++)
            {
                cur[0] = i;
                for (var j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    cur[j] = Math.Min(Math.Min(prev[j] + 1, cur[j - 1] + 1), prev[j - 1] + cost);
                }
                (prev, cur) = (cur, prev);
            }
            return prev[b.Length];
        }

        private sealed record CustomerFuzzyRecord(int Id, string FirstNameNorm, string LastNameNorm, string PhoneNorm, string AddressNorm);
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
