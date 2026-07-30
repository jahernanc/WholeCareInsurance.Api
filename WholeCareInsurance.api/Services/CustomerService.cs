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

        public async Task<IEnumerable<Customer>> GetAll(string? role = null)
        {
            var query = ApplyRoleFilter(
                _context.Customers
                    .Include(c => c.Policies)
                    .Include(c => c.Agent)
                    .Include(c => c.AssistantAgent)
                    .Include(c => c.RecordAgent)
                    .AsQueryable(),
                role);

            return await query.ToListAsync();
        }

        // Paginado del listado de Customers (§17) — usado solo por la pantalla de
        // administración; GetAll() sigue devolviendo la lista completa sin paginar
        // porque otras pantallas la usan como fuente para dropdowns/typeahead
        // (ej. selector de dependientes/titular en Policies.jsx).
        public async Task<(List<Customer> Items, int TotalCount)> Search(int page, int pageSize, string? role = null)
        {
            var query = ApplyRoleFilter(
                _context.Customers
                    .Include(c => c.Policies)
                    .Include(c => c.Agent)
                    .Include(c => c.AssistantAgent)
                    .Include(c => c.RecordAgent)
                    .AsQueryable(),
                role);

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

        public async Task<List<CustomerRelationship>> GetDependentsOf(int titularCustomerId)
            => await _context.CustomerRelationships
                .Include(r => r.DependentCustomer)
                .Where(r => r.TitularCustomerId == titularCustomerId)
                .ToListAsync();

        public async Task<List<CustomerRelationship>> GetTitularesOf(int dependentCustomerId)
            => await _context.CustomerRelationships
                .Include(r => r.TitularCustomer)
                .Where(r => r.DependentCustomerId == dependentCustomerId)
                .ToListAsync();

        // Crea el vínculo personal titular-dependiente si todavía no existe (§24.1) —
        // se invoca desde PoliciesController.AddDependent, además del PolicyDependent de
        // esa póliza puntual. Al remover un dependiente de una póliza NO se borra esta
        // relación (§24.6, confirmado por el responsable): la familia sigue siendo familia
        // aunque se dé de baja una cobertura puntual.
        public async Task UpsertRelationship(int titularCustomerId, int dependentCustomerId, string? relationshipType)
        {
            var exists = await _context.CustomerRelationships.AnyAsync(r =>
                r.TitularCustomerId == titularCustomerId && r.DependentCustomerId == dependentCustomerId);
            if (exists) return;

            _context.CustomerRelationships.Add(new CustomerRelationship
            {
                TitularCustomerId = titularCustomerId,
                DependentCustomerId = dependentCustomerId,
                RelationshipType = relationshipType,
                Source = "Sistema",
                CreatedAt = DateTime.UtcNow,
            });
            await _context.SaveChangesAsync();
        }

        public async Task<List<string>> GetDistinctCities()
            => await _context.Customers
                .Where(c => c.City != null && c.City != "")
                .Select(c => c.City!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

        // role: ver comentario en ICustomerService — "titular"/"dependiente" no son
        // excluyentes, cualquier otro valor (incluido null) no filtra nada.
        private IQueryable<Customer> ApplyRoleFilter(IQueryable<Customer> query, string? role) => role switch
        {
            "titular" => query.Where(c => c.Policies.Any()),
            "dependiente" => query.Where(c => _context.CustomerRelationships.Any(r => r.DependentCustomerId == c.Id)),
            _ => query,
        };
    }
}
