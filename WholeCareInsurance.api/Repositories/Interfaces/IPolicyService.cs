using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Services
{
    public interface IPolicyService
    {
        Task<IEnumerable<Policy>> GetAll();
        Task<List<Policy>> Search(int? customerId, string? firstName, string? lastName, string? policyNumber, string? status, string? type, int? insuranceCompanyId, int? period);
        Task<Policy?> GetById(int id);
        Task<Policy> Create(Policy policy);
        Task<Policy> Update(Policy policy);
        Task Delete(Policy policy);

        Task<List<PolicyDependent>> GetDependents(int policyId);
        Task<PolicyDependent?> GetDependent(int policyId, int customerId);
        Task<PolicyDependent> AddDependent(PolicyDependent dependent);
        Task<PolicyDependent> UpdateDependent(PolicyDependent dependent);
        Task RemoveDependent(PolicyDependent dependent);

        Task<List<PolicyDocument>> GetDocuments(int policyId);
        Task<PolicyDocument?> GetDocument(int policyId, int documentId);
        Task<PolicyDocument> AddDocument(PolicyDocument document);
        Task RemoveDocument(PolicyDocument document);
    }
}
