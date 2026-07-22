using Microsoft.EntityFrameworkCore;
using WholeCareInsurance.api.Data;
using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Services
{
    public class PolicyService : IPolicyService
    {
        private readonly AppDbContext _context;
        private readonly IPolicyDocumentStorage _documentStorage;

        public PolicyService(AppDbContext context, IPolicyDocumentStorage documentStorage)
        {
            _context = context;
            _documentStorage = documentStorage;
        }

        public async Task<IEnumerable<Policy>> GetAll()
            => await _context.Policies.Include(p => p.Customer).Include(p => p.InsuranceCompany).ToListAsync();

        public async Task<List<Policy>> Search(int? customerId, string? firstName, string? lastName, string? policyNumber, string? status, string? type, int? insuranceCompanyId, int? period)
        {
            var query = _context.Policies.Include(p => p.Customer).Include(p => p.InsuranceCompany).AsQueryable();

            if (customerId.HasValue)
                query = query.Where(p => p.CustomerId == customerId.Value);

            if (!string.IsNullOrWhiteSpace(firstName))
                query = query.Where(p => p.Customer.FirstName.Contains(firstName));

            if (!string.IsNullOrWhiteSpace(lastName))
                query = query.Where(p => p.Customer.LastName.Contains(lastName));

            if (!string.IsNullOrWhiteSpace(policyNumber))
                query = query.Where(p => p.PolicyNumber.Contains(policyNumber));

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(p => p.Status == status);

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(p => p.Type == type);

            if (insuranceCompanyId.HasValue)
                query = query.Where(p => p.InsuranceCompanyId == insuranceCompanyId.Value);

            if (period.HasValue)
                query = query.Where(p => p.Period == period.Value);

            return await query.ToListAsync();
        }

        public async Task<Policy?> GetById(int id)
            => await _context.Policies
                .Include(p => p.Customer)
                .Include(p => p.InsuranceCompany)
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
            _documentStorage.DeletePolicyFolder(policy.Id);
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

        public async Task<PolicyDependent> UpdateDependent(PolicyDependent dependent)
        {
            _context.PolicyDependents.Update(dependent);
            await _context.SaveChangesAsync();
            return dependent;
        }

        public async Task RemoveDependent(PolicyDependent dependent)
        {
            _context.PolicyDependents.Remove(dependent);
            await _context.SaveChangesAsync();
        }

        public async Task<List<PolicyDocument>> GetDocuments(int policyId)
            => await _context.PolicyDocuments
                .Where(d => d.PolicyId == policyId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

        public async Task<PolicyDocument?> GetDocument(int policyId, int documentId)
            => await _context.PolicyDocuments
                .FirstOrDefaultAsync(d => d.PolicyId == policyId && d.Id == documentId);

        public async Task<PolicyDocument> AddDocument(PolicyDocument document)
        {
            _context.PolicyDocuments.Add(document);
            await _context.SaveChangesAsync();
            return document;
        }

        public async Task RemoveDocument(PolicyDocument document)
        {
            _documentStorage.Delete(document.PolicyId, document.StoredFileName);
            _context.PolicyDocuments.Remove(document);
            await _context.SaveChangesAsync();
        }

        public async Task<List<PolicyBeneficiary>> GetBeneficiaries(int policyId)
            => await _context.PolicyBeneficiaries
                .Where(b => b.PolicyId == policyId)
                .ToListAsync();

        public async Task<PolicyBeneficiary?> GetBeneficiary(int policyId, int beneficiaryId)
            => await _context.PolicyBeneficiaries
                .FirstOrDefaultAsync(b => b.PolicyId == policyId && b.Id == beneficiaryId);

        public async Task<PolicyBeneficiary> AddBeneficiary(PolicyBeneficiary beneficiary)
        {
            _context.PolicyBeneficiaries.Add(beneficiary);
            await _context.SaveChangesAsync();
            return beneficiary;
        }

        public async Task RemoveBeneficiary(PolicyBeneficiary beneficiary)
        {
            _context.PolicyBeneficiaries.Remove(beneficiary);
            await _context.SaveChangesAsync();
        }
    }
}
