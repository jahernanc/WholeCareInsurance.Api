using Microsoft.EntityFrameworkCore;
using WholeCareInsurance.api.Data;
using WholeCareInsurance.api.DTOs.Dashboard;
using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _db;

        public DashboardService(AppDbContext db)
        {
            _db = db;
        }

        private IQueryable<Policy> ScopedPolicies(int? agentId, DateTime? from, DateTime? to)
        {
            var query = _db.Policies.Include(p => p.Customer).Include(p => p.InsuranceCompany).AsQueryable();

            if (agentId.HasValue)
                query = query.Where(p => p.Customer.AgentId == agentId.Value);

            // El filtro de fecha usa EffectiveDate (fuente de verdad, §1.11) — las pólizas
            // sin EffectiveDate quedan afuera cuando el filtro está activo (no hay alternativa
            // razonable), pero se cuentan igual en la vista sin filtrar.
            if (from.HasValue)
                query = query.Where(p => p.EffectiveDate != null && p.EffectiveDate >= from.Value);
            if (to.HasValue)
                query = query.Where(p => p.EffectiveDate != null && p.EffectiveDate <= to.Value);

            return query;
        }

        private async Task<Dictionary<int, int>> DependentsCountByPolicy(List<int> policyIds)
        {
            if (policyIds.Count == 0) return new Dictionary<int, int>();

            return await _db.PolicyDependents
                .Where(pd => policyIds.Contains(pd.PolicyId))
                .GroupBy(pd => pd.PolicyId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);
        }

        // Miembros de una póliza = 1 titular + sus dependientes (§9.3).
        private static int MembersOf(int policyId, Dictionary<int, int> dependentsByPolicy)
            => 1 + dependentsByPolicy.GetValueOrDefault(policyId, 0);

        // Personas físicas distintas del scope (§24.4): titulares + sus dependientes vía
        // CustomerRelationship, deduplicado por Customer.Id — a diferencia de MembersOf,
        // no cuenta dos veces a la misma persona si aparece en 2 pólizas del scope.
        private async Task<int> UniqueMembersCount(List<int> titularIds)
        {
            if (titularIds.Count == 0) return 0;

            var dependentIds = await _db.CustomerRelationships
                .Where(r => titularIds.Contains(r.TitularCustomerId))
                .Select(r => r.DependentCustomerId)
                .Distinct()
                .ToListAsync();

            return titularIds.Union(dependentIds).Count();
        }

        public async Task<DashboardSummaryDto> GetSummary(int? agentId, DateTime? from, DateTime? to)
        {
            var policies = await ScopedPolicies(agentId, from, to)
                .Select(p => new { p.Id, p.CustomerId })
                .ToListAsync();
            var policyIds = policies.Select(p => p.Id).ToList();
            var dependentsByPolicy = await DependentsCountByPolicy(policyIds);
            var membersCount = policyIds.Sum(id => MembersOf(id, dependentsByPolicy));

            var result = new DashboardSummaryDto
            {
                PoliciesCount = policies.Count,
                MembersCount = membersCount,
                UniqueMembersCount = await UniqueMembersCount(policies.Select(p => p.CustomerId).Distinct().ToList()),
            };

            // Agencias/Agentes: solo tienen sentido en la vista global (agentId == null) —
            // ni un Agente escalado a sí mismo ni un Admin "parado en los zapatos" de un
            // agente puntual deberían ver estos totales org-wide junto a un número scopeado.
            if (agentId == null)
            {
                result.AgenciesCount = await _db.Users
                    .Where(u => u.Agency != null)
                    .Select(u => u.Agency)
                    .Distinct()
                    .CountAsync();
                result.AgentsCount = await _db.Users.CountAsync(u => u.Rol == "Agente");
            }

            return result;
        }

        public async Task<List<DashboardStatusCountDto>> GetByStatus(int? agentId, DateTime? from, DateTime? to)
        {
            var policies = await ScopedPolicies(agentId, from, to)
                .Select(p => new { p.Id, p.Status })
                .ToListAsync();
            var dependentsByPolicy = await DependentsCountByPolicy(policies.Select(p => p.Id).ToList());

            return policies
                .GroupBy(p => p.Status)
                .Select(g => new DashboardStatusCountDto
                {
                    Status = g.Key,
                    PoliciesCount = g.Count(),
                    MembersCount = g.Sum(p => MembersOf(p.Id, dependentsByPolicy)),
                })
                .OrderByDescending(s => s.PoliciesCount)
                .ToList();
        }

        public async Task<List<DashboardTypeCountDto>> GetByType(int? agentId, DateTime? from, DateTime? to)
        {
            var policies = await ScopedPolicies(agentId, from, to)
                .Select(p => p.Type)
                .ToListAsync();

            return policies
                .GroupBy(t => t)
                .Select(g => new DashboardTypeCountDto { Type = g.Key, PoliciesCount = g.Count() })
                .OrderByDescending(t => t.PoliciesCount)
                .ToList();
        }

        public async Task<DashboardStatsDto> GetStats(int? agentId, DateTime? from, DateTime? to)
        {
            const string sinEspecificar = "Sin especificar";

            var policies = await ScopedPolicies(agentId, from, to)
                .Select(p => new
                {
                    InsuranceCompanyName = p.InsuranceCompany.Name,
                    County = p.Customer.County,
                    City = p.Customer.City,
                })
                .ToListAsync();

            return new DashboardStatsDto
            {
                ByInsuranceCompany = policies
                    .GroupBy(p => p.InsuranceCompanyName)
                    .Select(g => new DashboardNameCountDto { Name = g.Key, PoliciesCount = g.Count() })
                    .OrderByDescending(x => x.PoliciesCount)
                    .ToList(),
                ByCounty = policies
                    .GroupBy(p => string.IsNullOrWhiteSpace(p.County) ? sinEspecificar : p.County!)
                    .Select(g => new DashboardNameCountDto { Name = g.Key, PoliciesCount = g.Count() })
                    .OrderByDescending(x => x.PoliciesCount)
                    .ToList(),
                ByCity = policies
                    .GroupBy(p => string.IsNullOrWhiteSpace(p.City) ? sinEspecificar : p.City!)
                    .Select(g => new DashboardNameCountDto { Name = g.Key, PoliciesCount = g.Count() })
                    .OrderByDescending(x => x.PoliciesCount)
                    .ToList(),
            };
        }

        public async Task<List<DashboardLatestPolicyDto>> GetLatestPolicies(int? agentId)
        {
            var query = _db.Policies.Include(p => p.Customer).AsQueryable();
            if (agentId.HasValue)
                query = query.Where(p => p.Customer.AgentId == agentId.Value);

            return await query
                .OrderByDescending(p => p.UpdatedAt)
                .Take(10)
                .Select(p => new DashboardLatestPolicyDto
                {
                    Id = p.Id,
                    PolicyNumber = p.PolicyNumber,
                    CustomerId = p.CustomerId,
                    CustomerName = p.Customer.FirstName + " " + p.Customer.LastName,
                    Phone = p.Customer.Phone,
                    Email = p.Customer.Email,
                    Status = p.Status,
                    UpdatedAt = p.UpdatedAt,
                })
                .ToListAsync();
        }

        public async Task<List<DashboardUpcoming65Dto>> GetUpcoming65(int? agentId)
        {
            var today = DateTime.UtcNow.Date;

            // Pre-filtro angosto en SQL (por año de nacimiento) para no traer toda la tabla
            // de Customers — el chequeo exacto de la ventana ±4 meses se hace en memoria,
            // porque AddYears/AddMonths no traduce de forma confiable a SQL vía EF Core.
            var minBirthYear = today.Year - 66;
            var maxBirthYear = today.Year - 64;

            var query = _db.Customers.AsQueryable();
            if (agentId.HasValue)
                query = query.Where(c => c.AgentId == agentId.Value);

            var candidates = await query
                .Where(c => c.DateOfBirth.Year >= minBirthYear && c.DateOfBirth.Year <= maxBirthYear)
                .Select(c => new { c.Id, c.FirstName, c.LastName, c.DateOfBirth })
                .ToListAsync();

            var result = new List<DashboardUpcoming65Dto>();
            foreach (var c in candidates)
            {
                var turns65 = c.DateOfBirth.AddYears(65);
                var windowStart = turns65.AddMonths(-4);
                var windowEnd = turns65.AddMonths(4);

                if (today >= windowStart && today <= windowEnd)
                {
                    result.Add(new DashboardUpcoming65Dto
                    {
                        CustomerId = c.Id,
                        CustomerName = $"{c.FirstName} {c.LastName}",
                        DateOfBirth = c.DateOfBirth,
                        Age = CalculateAge(c.DateOfBirth, today),
                    });
                }
            }

            return result.OrderBy(r => r.DateOfBirth).ToList();
        }

        private static int CalculateAge(DateTime dateOfBirth, DateTime asOf)
        {
            var age = asOf.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > asOf.AddYears(-age)) age--;
            return age;
        }
    }
}
