using Microsoft.EntityFrameworkCore;
using WholeCareInsurance.api.Data;
using WholeCareInsurance.api.Models;
using WholeCareInsurance.api.Services;
using WholeCareInsurance.Migration.Reporting;

namespace WholeCareInsurance.Migration.Importers
{
    public class HistoryTransition
    {
        public string? OldStatus { get; init; }
        public required string NewStatus { get; init; }
        public required DateTime ChangedAt { get; init; }
    }

    public class DependentLink
    {
        public required int CustomerId { get; init; }
        public required bool IsAplicante { get; init; }
    }

    // Persiste una Policy consolidada (+ su historial + sus dependientes) de forma
    // idempotente: si ya existe una Policy con ese PolicyNumber, se saltea entera (no
    // se toca nada) — así --commit se puede reintentar después de resolver un problema
    // puntual sin reprocesar lo que ya entró bien. El llamador controla el alcance de
    // la transacción (una por Policy en --commit; un savepoint por Policy en --dry-run).
    public static class PolicyPersistence
    {
        public static async Task<bool> PersistAsync(
            AppDbContext db,
            MigrationReport report,
            Policy policy,
            List<HistoryTransition> historyTransitions,
            List<DependentLink> dependents,
            int sourceRowCount,
            List<string> sourceReferences,
            string sourceFile,
            string customerName,
            string companyName)
        {
            var existing = await db.Policies
                .Include(p => p.Customer)
                .FirstOrDefaultAsync(p => p.PolicyNumber == policy.PolicyNumber);
            if (existing != null)
            {
                report.AddPolicySkipped(policy.Type);
                report.SkippedDuplicatePolicyNumbers.Add(new SkippedDuplicateEntry
                {
                    PolicyNumber = policy.PolicyNumber,
                    ThisGroupCustomerName = customerName,
                    ThisGroupCustomerId = policy.CustomerId,
                    SourceReferences = sourceReferences,
                    ExistingPolicyId = existing.Id,
                    ExistingPolicyCustomerId = existing.CustomerId,
                    ExistingPolicyCustomerName = $"{existing.Customer.FirstName} {existing.Customer.LastName}",
                });
                return false;
            }

            db.Policies.Add(policy);
            await db.SaveChangesAsync();

            foreach (var dep in dependents)
                db.PolicyDependents.Add(new PolicyDependent
                {
                    PolicyId = policy.Id,
                    CustomerId = dep.CustomerId,
                    IsAplicante = dep.IsAplicante,
                });

            var historyService = new PolicyHistoryService(db);
            var entries = historyTransitions.Select(t => new PolicyHistory
            {
                PolicyId = policy.Id,
                FieldChanged = "Status",
                OldValue = t.OldStatus,
                NewValue = t.NewStatus,
                ChangedAt = t.ChangedAt,
                ChangedByUserId = null,
                Source = "Migración",
            }).ToList();
            await historyService.AddBulk(entries);

            await db.SaveChangesAsync();

            report.AddPolicyCreated(policy.Type);
            report.HistoryEntriesCreated += entries.Count;
            report.HistoryGroupsConsolidated += 1;
            report.PolicyGroups.Add(new PolicyGroupSummary
            {
                SourceFile = sourceFile,
                CustomerName = customerName,
                CompanyName = companyName,
                PolicyNumber = policy.PolicyNumber,
                SourceRowCount = sourceRowCount,
                SourceReferences = sourceReferences,
                CurrentStatus = policy.Status,
            });

            return true;
        }
    }
}
