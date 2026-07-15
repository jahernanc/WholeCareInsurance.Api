using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Data.Configurations
{
    public class InsuranceCompanyConfiguration : IEntityTypeConfiguration<InsuranceCompany>
    {
        public void Configure(EntityTypeBuilder<InsuranceCompany> entity)
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                  .IsRequired()
                  .HasMaxLength(150);

            entity.HasIndex(c => c.Name).IsUnique();

            entity.Property(c => c.IsActive).IsRequired();
        }
    }
}
