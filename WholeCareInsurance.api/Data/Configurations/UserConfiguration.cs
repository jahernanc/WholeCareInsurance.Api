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

            // Refresh token hash (SHA-256 hex = 64 chars)
            entity.Property(u => u.RefreshTokenHash)
                  .HasMaxLength(64);

            entity.HasIndex(u => u.RefreshTokenHash)
                  .HasFilter("[RefreshTokenHash] IS NOT NULL");

            entity.Property(u => u.RefreshTokenExpiresAt);
        }
    }
}
