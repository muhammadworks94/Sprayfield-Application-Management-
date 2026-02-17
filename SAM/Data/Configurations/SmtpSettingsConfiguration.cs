using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class SmtpSettingsConfiguration : IEntityTypeConfiguration<SmtpSettings>
{
    public void Configure(EntityTypeBuilder<SmtpSettings> builder)
    {
        builder.ToTable("SmtpSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ScopeKey)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("SMTP_GLOBAL");

        builder.HasIndex(s => s.ScopeKey)
            .IsUnique();

        builder.Property(s => s.Host)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(s => s.Port)
            .IsRequired();

        builder.Property(s => s.Username)
            .HasMaxLength(256);

        builder.Property(s => s.PasswordEncrypted)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(s => s.FromEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(s => s.EnableSsl)
            .IsRequired()
            .HasDefaultValue(true);
    }
}
