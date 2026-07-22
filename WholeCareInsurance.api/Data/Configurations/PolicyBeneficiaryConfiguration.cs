using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Data.Configurations
{
    public class PolicyBeneficiaryConfiguration : IEntityTypeConfiguration<PolicyBeneficiary>
    {
        public void Configure(EntityTypeBuilder<PolicyBeneficiary> entity)
        {
            entity.HasKey(b => b.Id);

            entity.Property(b => b.TypeOfRelationship).IsRequired().HasMaxLength(50);
            entity.Property(b => b.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(b => b.LastName).IsRequired().HasMaxLength(100);
            entity.Property(b => b.Gender).HasMaxLength(20);
            entity.Property(b => b.Phone).HasMaxLength(20);
            entity.Property(b => b.Email).HasMaxLength(200);
            entity.Property(b => b.SocialSecurityNumber).HasMaxLength(20);

            entity.HasOne(b => b.Policy)
                  .WithMany()
                  .HasForeignKey(b => b.PolicyId)
                  .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
