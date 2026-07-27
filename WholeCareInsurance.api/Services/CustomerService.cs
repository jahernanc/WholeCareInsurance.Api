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
            => await _context.Customers
                .Include(c => c.Policies)
                .Include(c => c.Agent)
                .Include(c => c.AssistantAgent)
                .Include(c => c.RecordAgent)
                .ToListAsync();

        // Paginado del listado de Customers (§17) — usado solo por la pantalla de
        // administración; GetAll() sigue devolviendo la lista completa sin paginar
        // porque otras pantallas la usan como fuente para dropdowns/typeahead
        // (ej. selector de dependientes/titular en Policies.jsx).
        public async Task<(List<Customer> Items, int TotalCount)> Search(int page, int pageSize)
        {
            var query = _context.Customers
                .Include(c => c.Policies)
                .Include(c => c.Agent)
                .Include(c => c.AssistantAgent)
                .Include(c => c.RecordAgent)
                .AsQueryable();

            var totalCount = await query.CountAsync();

            pageSize = Math.Clamp(pageSize, 1, 100);
            if (page < 1) page = 1;

            var items = await query
                .OrderByDescending(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Customer?> GetById(int id)
            => await _context.Customers
                .Include(c => c.Policies)
                .Include(c => c.Agent)
                .Include(c => c.AssistantAgent)
                .Include(c => c.RecordAgent)
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
