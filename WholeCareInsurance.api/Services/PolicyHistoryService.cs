using Microsoft.EntityFrameworkCore;
using WholeCareInsurance.api.Data;
using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Services
{
    public class PolicyHistoryService : IPolicyHistoryService
    {
        private readonly AppDbContext _context;

        public PolicyHistoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PolicyHistory>> GetForPolicy(int policyId)
            => await _context.PolicyHistories
                .Include(h => h.ChangedByUser)
                .Where(h => h.PolicyId == policyId)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();

        public async Task RecordStatusChange(int policyId, string? oldStatus, string newStatus, int? changedByUserId, string source = "Sistema")
        {
            if (oldStatus == newStatus) return;

            _context.PolicyHistories.Add(new PolicyHistory
            {
                PolicyId = policyId,
                FieldChanged = "Status",
                OldValue = oldStatus,
                NewValue = newStatus,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = changedByUserId,
                Source = source
            });

            await _context.SaveChangesAsync();
        }

        public async Task AddBulk(IEnumerable<PolicyHistory> entries)
        {
            _context.PolicyHistories.AddRange(entries);
            await _context.SaveChangesAsync();
        }
    }
}
