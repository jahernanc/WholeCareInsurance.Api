using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Data.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> entity)
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.SocialSecurityNumber).IsRequired().HasMaxLength(20);
            entity.HasIndex(c => c.SocialSecurityNumber).IsUnique();

            entity.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(c => c.LastName).IsRequired().HasMaxLength(100);
            entity.Property(c => c.DateOfBirth).IsRequired();

            entity.Property(c => c.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(c => c.Email).IsUnique();

            entity.Property(c => c.Address).IsRequired().HasMaxLength(300);
            entity.Property(c => c.Phone).IsRequired().HasMaxLength(20);
            entity.Property(c => c.MigrationStatus).IsRequired().HasMaxLength(50);
            entity.Property(c => c.RelacionConPrincipal).IsRequired().HasMaxLength(50);

            entity.HasMany(c => c.Policies)
                  .WithOne(p => p.Customer)
                  .HasForeignKey(p => p.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
