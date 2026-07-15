using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Services
{
    public interface IInsuranceCompanyService
    {
        Task<IEnumerable<InsuranceCompany>> GetAll();
        Task<InsuranceCompany?> GetById(int id);
        Task<InsuranceCompany?> GetByName(string name);
        Task<InsuranceCompany> Create(InsuranceCompany company);
        Task<InsuranceCompany> Update(InsuranceCompany company);
    }
}
