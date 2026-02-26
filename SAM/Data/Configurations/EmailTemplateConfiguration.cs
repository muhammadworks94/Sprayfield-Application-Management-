using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("EmailTemplates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TemplateKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(t => t.TemplateKey)
            .IsUnique();

        builder.Property(t => t.DisplayName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.SubjectTemplate)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.BodyTemplate)
            .IsRequired()
            .HasMaxLength(8000);

        builder.Property(t => t.IsSystemTemplate)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}
