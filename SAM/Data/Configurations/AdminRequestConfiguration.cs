using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class AdminRequestConfiguration : IEntityTypeConfiguration<AdminRequest>
{
    public void Configure(EntityTypeBuilder<AdminRequest> builder)
    {
        builder.ToTable("AdminRequests");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.RequestType)
            .HasConversion<int>();

        builder.Property(a => a.TargetEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(a => a.TargetFullName)
            .HasMaxLength(200);

        builder.Property(a => a.Justification)
            .HasMaxLength(2000);

        builder.Property(a => a.RequestedByEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(a => a.Status)
            .HasConversion<int>();

        builder.HasIndex(a => a.TargetEmail);
        builder.HasIndex(a => a.Status);
    }
}


