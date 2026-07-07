using Microsoft.EntityFrameworkCore;
using WholeCareInsurance.api.Data;
using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Services
{
    public class PolicyService : IPolicyService
    {
        private readonly AppDbContext _context;

        public PolicyService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Policy>> GetAll()
            => await _context.Policies.Include(p => p.Customer).ToListAsync();

        public async Task<Policy?> GetById(int id)
            => await _context.Policies
                .Include(p => p.Customer)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Policy> Create(Policy policy)
        {
            _context.Policies.Add(policy);
            await _context.SaveChangesAsync();
            return policy;
        }

        public async Task<Policy> Update(Policy policy)
        {
            _context.Policies.Update(policy);
            await _context.SaveChangesAsync();
            return policy;
        }

        public async Task Delete(Policy policy)
        {
            _context.Policies.Remove(policy);
            await _context.SaveChangesAsync();
        }

        public async Task<List<PolicyDependent>> GetDependents(int policyId)
            => await _context.PolicyDependents
                .Include(pd => pd.Customer)
                .Where(pd => pd.PolicyId == policyId)
                .ToListAsync();

        public async Task<PolicyDependent?> GetDependent(int policyId, int customerId)
            => await _context.PolicyDependents
                .Include(pd => pd.Customer)
                .FirstOrDefaultAsync(pd => pd.PolicyId == policyId && pd.CustomerId == customerId);

        public async Task<PolicyDependent> AddDependent(PolicyDependent dependent)
        {
            _context.PolicyDependents.Add(dependent);
            await _context.SaveChangesAsync();
            return dependent;
        }

        public async Task RemoveDependent(PolicyDependent dependent)
        {
            _context.PolicyDependents.Remove(dependent);
            await _context.SaveChangesAsync();
        }
    }
}
