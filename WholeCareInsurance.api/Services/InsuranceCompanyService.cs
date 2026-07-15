using Microsoft.EntityFrameworkCore;
using WholeCareInsurance.api.Data;
using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Services
{
    public class InsuranceCompanyService : IInsuranceCompanyService
    {
        private readonly AppDbContext _context;

        public InsuranceCompanyService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<InsuranceCompany>> GetAll()
            => await _context.InsuranceCompanies.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

        public async Task<InsuranceCompany?> GetById(int id)
            => await _context.InsuranceCompanies.FindAsync(id);

        public async Task<InsuranceCompany?> GetByName(string name)
            => await _context.InsuranceCompanies.FirstOrDefaultAsync(c => c.Name == name);

        public async Task<InsuranceCompany> Create(InsuranceCompany company)
        {
            _context.InsuranceCompanies.Add(company);
            await _context.SaveChangesAsync();
            return company;
        }

        public async Task<InsuranceCompany> Update(InsuranceCompany company)
        {
            _context.InsuranceCompanies.Update(company);
            await _context.SaveChangesAsync();
            return company;
        }
    }
}
