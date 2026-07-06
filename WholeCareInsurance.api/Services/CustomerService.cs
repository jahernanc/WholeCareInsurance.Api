using Microsoft.EntityFrameworkCore;
using WholeCareInsurance.api.Data;
using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly AppDbContext _context;

        public CustomerService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Customer>> GetAll()
            => await _context.Customers.Include(c => c.Policies).ToListAsync();

        public async Task<Customer?> GetById(int id)
            => await _context.Customers
                .Include(c => c.Policies)
                .FirstOrDefaultAsync(c => c.Id == id);

        public async Task<Customer> Create(Customer customer)
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task<Customer> Update(Customer customer)
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task Delete(Customer customer)
        {
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
        }
    }
}
