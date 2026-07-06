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

            entity.Property(c => c.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(c => c.Email)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.HasIndex(c => c.Email).IsUnique();

            entity.Property(c => c.DocumentNumber)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.HasIndex(c => c.DocumentNumber).IsUnique();

            entity.HasMany(c => c.Policies)
                  .WithOne(p => p.Customer)
                  .HasForeignKey(p => p.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
