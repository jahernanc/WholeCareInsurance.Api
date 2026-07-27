using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Services
{
    public interface ICustomerService
    {
        Task<IEnumerable<Customer>> GetAll();
        Task<(List<Customer> Items, int TotalCount)> Search(int page, int pageSize);
        Task<Customer?> GetById(int id);
        Task<Customer> Create(Customer customer);
        Task<Customer> Update(Customer customer);
        Task Delete(Customer customer);
    }
}
