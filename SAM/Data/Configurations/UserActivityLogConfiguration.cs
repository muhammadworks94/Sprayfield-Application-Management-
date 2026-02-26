using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class UserActivityLogConfiguration : IEntityTypeConfiguration<UserActivityLog>
{
    public void Configure(EntityTypeBuilder<UserActivityLog> builder)
    {
        builder.ToTable("UserActivityLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OccurredAtUtc)
            .IsRequired();

        builder.Property(x => x.Module)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.EntityId)
            .HasMaxLength(128);

        builder.Property(x => x.ActorUserId)
            .HasMaxLength(450);

        builder.Property(x => x.ActorEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.ActorDisplayName)
            .HasMaxLength(200);

        builder.Property(x => x.HttpMethod)
            .HasMaxLength(16);

        builder.Property(x => x.Path)
            .HasMaxLength(1024);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(64);

        builder.Property(x => x.Summary)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(128);

        builder.HasIndex(x => x.OccurredAtUtc);
        builder.HasIndex(x => new { x.CompanyId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.ActorUserId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.ActivityType, x.OccurredAtUtc });
    }
}

