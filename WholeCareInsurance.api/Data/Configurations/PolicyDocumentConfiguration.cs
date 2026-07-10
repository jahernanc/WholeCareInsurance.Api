using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WholeCareInsurance.api.Models;

namespace WholeCareInsurance.api.Data.Configurations
{
    public class PolicyDocumentConfiguration : IEntityTypeConfiguration<PolicyDocument>
    {
        public void Configure(EntityTypeBuilder<PolicyDocument> entity)
        {
            entity.HasKey(d => d.Id);

            entity.Property(d => d.OriginalFileName).IsRequired().HasMaxLength(300);
            entity.Property(d => d.StoredFileName).IsRequired().HasMaxLength(100);
            entity.Property(d => d.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(d => d.SizeBytes).IsRequired();
            entity.Property(d => d.UploadedAt).IsRequired();

            entity.HasOne(d => d.Policy)
                  .WithMany()
                  .HasForeignKey(d => d.PolicyId)
                  .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
