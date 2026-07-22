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

            entity.Property(p => p.MedicaidLevel).HasMaxLength(100);
            entity.Property(p => p.MedicalCorporation).HasMaxLength(200);

            entity.Property(p => p.AdditionalOrAlternatePolicyDetail).HasMaxLength(300);
            entity.Property(p => p.UnderwritingRequirements).HasMaxLength(500);
            entity.Property(p => p.BillingType).HasMaxLength(50);
            entity.Property(p => p.PremiumFrequency).HasMaxLength(50);
            entity.Property(p => p.PlannedPeriodicModalPremium).HasColumnType("decimal(18,2)");
            entity.Property(p => p.SourceOfFunds).HasMaxLength(200);
            entity.Property(p => p.PhysicianName).HasMaxLength(200);
            entity.Property(p => p.PhysicianAddress).HasMaxLength(300);
            entity.Property(p => p.AdditionalInformation).HasMaxLength(2000);

            entity.Property(p => p.BankAccountType).HasMaxLength(20);
            entity.Property(p => p.RoutingNumber).HasMaxLength(20);
            entity.Property(p => p.AccountNumber).HasMaxLength(20);
            entity.Property(p => p.RepresentativeName).HasMaxLength(200);
            entity.Property(p => p.RepresentativeRelationship).HasMaxLength(100);
        }

    }
}
