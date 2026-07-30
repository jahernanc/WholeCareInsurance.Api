using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Data.Configurations
{
    public class CustomerRelationshipConfiguration : IEntityTypeConfiguration<CustomerRelationship>
    {
        public void Configure(EntityTypeBuilder<CustomerRelationship> entity)
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.RelationshipType).HasMaxLength(50);
            entity.Property(r => r.Source).IsRequired().HasMaxLength(20);
            entity.Property(r => r.CreatedAt).IsRequired();

            entity.HasIndex(r => new { r.TitularCustomerId, r.DependentCustomerId }).IsUnique();

            // Restrict en las dos FKs (mismo motivo que PolicyDependentConfiguration.cs:18-20):
            // dos FKs a la misma tabla Customer desde una tabla intermedia no pueden
            // cascadear ambas sin que SQL Server rechace el constraint por múltiples
            // paths de cascada.
            entity.HasOne(r => r.TitularCustomer)
                  .WithMany()
                  .HasForeignKey(r => r.TitularCustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.DependentCustomer)
                  .WithMany()
                  .HasForeignKey(r => r.DependentCustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
