using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Services
{
    public interface ICustomerService
    {
        Task<IEnumerable<Customer>> GetAll();
        Task<Customer?> GetById(int id);
        Task<Customer> Create(Customer customer);
        Task<Customer> Update(Customer customer);
        Task Delete(Customer customer);
    }
}
