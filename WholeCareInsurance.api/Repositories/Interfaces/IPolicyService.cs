using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Services
{
    public interface IPolicyService
    {
        Task<IEnumerable<Policy>> GetAll();
        Task<Policy?> GetById(int id);
        Task<Policy> Create(Policy policy);
        Task<Policy> Update(Policy policy);
        Task Delete(Policy policy);
    }
}
