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

            entity.HasOne(p => p.InsuranceCompany)
                  .WithMany()
                  .HasForeignKey(p => p.InsuranceCompanyId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(p => p.Status)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(p => p.Period)
                  .IsRequired();

            entity.Property(p => p.Premium)
                  .HasColumnType("decimal(18,2)");

            entity.Property(p => p.StartDate).IsRequired();
            entity.Property(p => p.EndDate).IsRequired();

            entity.Property(p => p.PlanType).HasMaxLength(20);
            entity.Property(p => p.InsurancePlan).HasMaxLength(200);
            entity.Property(p => p.EffectiveDate);
            entity.Property(p => p.TaxCreditSubsidy).HasColumnType("decimal(18,2)");
            entity.Property(p => p.MonthlyPremiumAmount).HasColumnType("decimal(18,2)");
        }

    }
}
