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

            entity.Property(c => c.Address1).IsRequired().HasMaxLength(300);
            entity.Property(c => c.Phone).IsRequired().HasMaxLength(20);
            entity.Property(c => c.MigrationStatus).IsRequired().HasMaxLength(50);
            entity.Property(c => c.RelacionConPrincipal).IsRequired().HasMaxLength(50);

            entity.Property(c => c.ZipCode).HasMaxLength(10);
            entity.Property(c => c.State).HasMaxLength(2);
            entity.Property(c => c.City).HasMaxLength(100);
            entity.Property(c => c.County).HasMaxLength(100);
            entity.Property(c => c.MaritalStatus).HasMaxLength(20);
            entity.Property(c => c.Occupation).HasMaxLength(100);

            entity.Property(c => c.MiddleName).HasMaxLength(100);
            entity.Property(c => c.Gender).HasMaxLength(20);
            entity.Property(c => c.GreenCard).HasMaxLength(50);
            entity.Property(c => c.WorkPermit).HasMaxLength(50);
            entity.Property(c => c.Address2).HasMaxLength(300);
            entity.Property(c => c.EmployerName).HasMaxLength(150);
            entity.Property(c => c.CompanyPhone).HasMaxLength(20);
            entity.Property(c => c.AnnualIncome).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(c => c.Tags).HasMaxLength(500);
            entity.Property(c => c.ContactLanguage).HasMaxLength(20);

            entity.HasMany(c => c.Policies)
                  .WithOne(p => p.Customer)
                  .HasForeignKey(p => p.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Tres FKs distintas a User (misma tabla) -> Restrict para no arrastrar
            // el borrado de un agente a los Customers que todavía lo referencian.
            entity.HasOne(c => c.Agent)
                  .WithMany()
                  .HasForeignKey(c => c.AgentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.AssistantAgent)
                  .WithMany()
                  .HasForeignKey(c => c.AssistantAgentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.RecordAgent)
                  .WithMany()
                  .HasForeignKey(c => c.RecordAgentId)
                  .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
