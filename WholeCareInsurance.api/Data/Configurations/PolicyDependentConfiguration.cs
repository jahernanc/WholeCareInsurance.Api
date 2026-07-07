using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Data.Configurations
{
    public class PolicyDependentConfiguration : IEntityTypeConfiguration<PolicyDependent>
    {
        public void Configure(EntityTypeBuilder<PolicyDependent> entity)
        {
            entity.HasKey(pd => new { pd.PolicyId, pd.CustomerId });

            entity.HasOne(pd => pd.Policy)
                  .WithMany()
                  .HasForeignKey(pd => pd.PolicyId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Restrict, not Cascade: Customer already cascades to Policy via the titular FK,
            // so a second cascade path through this table would give SQL Server two cascade
            // paths from Customer and it refuses to create the constraint.
            entity.HasOne(pd => pd.Customer)
                  .WithMany()
                  .HasForeignKey(pd => pd.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
