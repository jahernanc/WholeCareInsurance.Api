using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> entity)
        {
            // PK explícita
            entity.HasKey(u => u.Id);

            // ✅ Email
            entity.Property(u => u.Email)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.HasIndex(u => u.Email)
                  .IsUnique();

            // ✅ Nombre
            entity.Property(u => u.Nombre)
                  .IsRequired()
                  .HasMaxLength(100);

            // ✅ PasswordHash
            entity.Property(u => u.PasswordHash)
                  .IsRequired()
                  .HasMaxLength(500);

            // ✅ Rol
            entity.Property(u => u.Rol)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(u => u.IsEncargado)
                  .IsRequired();

            entity.Property(u => u.PreferredLanguage)
                  .IsRequired()
                  .HasMaxLength(5);

            // Datos de perfil del Agente (§11)
            entity.Property(u => u.MiddleName).HasMaxLength(100);
            entity.Property(u => u.Gender).HasMaxLength(20);
            entity.Property(u => u.Address1).HasMaxLength(300);
            entity.Property(u => u.Address2).HasMaxLength(300);
            entity.Property(u => u.City).HasMaxLength(100);
            entity.Property(u => u.ZipCode).HasMaxLength(10);
            entity.Property(u => u.State).HasMaxLength(2);
            entity.Property(u => u.County).HasMaxLength(100);

            entity.Property(u => u.Licensed).IsRequired();
            entity.Property(u => u.LicenseNumber).HasMaxLength(50);

            entity.Property(u => u.NpnNumber).HasMaxLength(50);
            entity.Property(u => u.NpnOverride).IsRequired();

            entity.Property(u => u.HasCompanyContract).IsRequired();
            entity.Property(u => u.ContractNumber).HasMaxLength(50);
            entity.Property(u => u.CompanyName).HasMaxLength(150);

            entity.Property(u => u.ContractsWanted).HasMaxLength(200);
            entity.Property(u => u.AdditionalInformation).HasMaxLength(1000);

            entity.Property(u => u.TermsAccepted).IsRequired();

            // Refresh token hash (SHA-256 hex = 64 chars)
            entity.Property(u => u.RefreshTokenHash)
                  .HasMaxLength(64);

            entity.HasIndex(u => u.RefreshTokenHash)
                  .HasFilter("[RefreshTokenHash] IS NOT NULL");

            entity.Property(u => u.RefreshTokenExpiresAt);

            entity.Property(u => u.MustChangePassword)
                  .IsRequired();

            // Password reset token hash (SHA-256 hex = 64 chars), mismo patrón que RefreshTokenHash
            entity.Property(u => u.PasswordResetTokenHash)
                  .HasMaxLength(64);

            entity.HasIndex(u => u.PasswordResetTokenHash)
                  .HasFilter("[PasswordResetTokenHash] IS NOT NULL");

            entity.Property(u => u.PasswordResetTokenExpiresAt);
        }
    }
}
