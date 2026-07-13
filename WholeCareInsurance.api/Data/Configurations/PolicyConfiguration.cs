using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Data.Configurations
{
    public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
    {

        public void Configure(EntityTypeBuilder<Policy> entity)
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.PolicyNumber)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.HasIndex(p => p.PolicyNumber).IsUnique();

            entity.Property(p => p.Type)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(p => p.InsuranceCompany)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(p => p.Status)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(p => p.Period)
                  .IsRequired();

            entity.Property(p => p.Premium)
                  .HasColumnType("decimal(18,2)");

            entity.Property(p => p.StartDate).IsRequired();
            entity.Property(p => p.EndDate).IsRequired();
        }

    }
}
