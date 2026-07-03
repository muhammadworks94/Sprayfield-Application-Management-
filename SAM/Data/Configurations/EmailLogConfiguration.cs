using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("EmailLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SentAtUtc).IsRequired();
        builder.Property(x => x.ToEmail).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Subject).IsRequired().HasMaxLength(500);
        builder.Property(x => x.TemplateKey).HasMaxLength(100);
        builder.Property(x => x.TemplateDisplayName).HasMaxLength(150);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.HtmlBody).HasMaxLength(8000);
        builder.Property(x => x.InitiatedByUserId).HasMaxLength(450);
        builder.Property(x => x.InitiatedByEmail).HasMaxLength(256);
        builder.Property(x => x.InitiatedByDisplayName).HasMaxLength(200);

        builder.HasIndex(x => x.SentAtUtc);
        builder.HasIndex(x => new { x.Status, x.SentAtUtc });
        builder.HasIndex(x => new { x.TemplateKey, x.SentAtUtc });
        builder.HasIndex(x => new { x.ToEmail, x.SentAtUtc });
    }
}
