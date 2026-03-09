using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class ApplicationZoneConfiguration : IEntityTypeConfiguration<ApplicationZone>
{
    public void Configure(EntityTypeBuilder<ApplicationZone> builder)
    {
        builder.ToTable("ApplicationZones");

        builder.HasKey(z => z.Id);

        builder.Property(z => z.ZoneName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(z => z.PercentOfField)
            .HasColumnType("decimal(9,4)");

        builder.Property(z => z.Acres)
            .HasColumnType("decimal(18,4)");

        builder.HasIndex(z => z.CompanyId);
        builder.HasIndex(z => z.SprayfieldId);
        builder.HasIndex(z => new { z.SprayfieldId, z.ZoneName }).IsUnique();

        builder.HasOne(z => z.Company)
            .WithMany()
            .HasForeignKey(z => z.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(z => z.Sprayfield)
            .WithMany(s => s.ApplicationZones)
            .HasForeignKey(z => z.SprayfieldId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(z => z.Soil)
            .WithMany(s => s.ApplicationZones)
            .HasForeignKey(z => z.SoilId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(z => z.Nozzle)
            .WithMany(n => n.ApplicationZones)
            .HasForeignKey(z => z.NozzleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(z => z.Crop)
            .WithMany(c => c.ApplicationZones)
            .HasForeignKey(z => z.CropId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
