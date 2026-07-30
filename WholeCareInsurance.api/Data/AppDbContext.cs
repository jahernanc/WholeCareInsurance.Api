using Microsoft.EntityFrameworkCore;
using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Data
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Policy> Policies => Set<Policy>();
        public DbSet<PolicyDependent> PolicyDependents => Set<PolicyDependent>();
        public DbSet<PolicyDocument> PolicyDocuments => Set<PolicyDocument>();
        public DbSet<PolicyBeneficiary> PolicyBeneficiaries => Set<PolicyBeneficiary>();
        public DbSet<PolicyHistory> PolicyHistories => Set<PolicyHistory>();
        public DbSet<InsuranceCompany> InsuranceCompanies => Set<InsuranceCompany>();
        public DbSet<CustomerRelationship> CustomerRelationships => Set<CustomerRelationship>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        }
    }

}
