using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class ErrorLogConfiguration : IEntityTypeConfiguration<ErrorLog>
{
    public void Configure(EntityTypeBuilder<ErrorLog> builder)
    {
        builder.ToTable("ErrorLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.Property(x => x.ExceptionType).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.Module).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Path).HasMaxLength(1024);
        builder.Property(x => x.HttpMethod).HasMaxLength(16);
        builder.Property(x => x.RequestId).HasMaxLength(128);
        builder.Property(x => x.CorrelationId).HasMaxLength(128);
        builder.Property(x => x.ActorUserId).HasMaxLength(450);
        builder.Property(x => x.ActorEmail).HasMaxLength(256);
        builder.Property(x => x.ActorDisplayName).HasMaxLength(200);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.QueryString).HasMaxLength(1024);
        builder.Property(x => x.InnerExceptionType).HasMaxLength(300);
        builder.Property(x => x.InnerExceptionMessage).HasMaxLength(2000);

        builder.HasIndex(x => x.OccurredAtUtc);
        builder.HasIndex(x => new { x.StatusCode, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.CompanyId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.ExceptionType, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.ActorUserId, x.OccurredAtUtc });
    }
}

