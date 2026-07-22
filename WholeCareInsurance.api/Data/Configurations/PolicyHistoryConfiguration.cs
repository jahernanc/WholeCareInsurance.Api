using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Data.Configurations
{
    public class PolicyHistoryConfiguration : IEntityTypeConfiguration<PolicyHistory>
    {
        public void Configure(EntityTypeBuilder<PolicyHistory> entity)
        {
            entity.HasKey(h => h.Id);

            entity.Property(h => h.FieldChanged).IsRequired().HasMaxLength(100);
            entity.Property(h => h.OldValue).HasMaxLength(1000);
            entity.Property(h => h.NewValue).HasMaxLength(1000);
            entity.Property(h => h.ChangedAt).IsRequired();
            entity.Property(h => h.Source).IsRequired().HasMaxLength(20);

            entity.HasOne(h => h.Policy)
                  .WithMany()
                  .HasForeignKey(h => h.PolicyId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Restrict, mismo criterio que Customer.AgentId — hoy es indistinto
            // porque UsersController no tiene endpoint de borrado de usuarios.
            entity.HasOne(h => h.ChangedByUser)
                  .WithMany()
                  .HasForeignKey(h => h.ChangedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
