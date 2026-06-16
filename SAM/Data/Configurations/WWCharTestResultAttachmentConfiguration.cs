using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class WWCharTestResultAttachmentConfiguration : IEntityTypeConfiguration<WWCharTestResultAttachment>
{
    public void Configure(EntityTypeBuilder<WWCharTestResultAttachment> builder)
    {
        builder.ToTable("WWCharTestResultAttachments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileStoragePath).IsRequired().HasMaxLength(500);
        builder.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(260);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.UploadedBy).IsRequired().HasMaxLength(256);
        builder.Property(x => x.TestDate).HasColumnType("date");

        builder.HasIndex(x => x.WWCharId);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.TestDate);

        builder.HasOne(x => x.WWChar)
            .WithMany(x => x.TestResultAttachments)
            .HasForeignKey(x => x.WWCharId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
