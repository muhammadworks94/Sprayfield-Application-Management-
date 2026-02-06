using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;
using SAM.Domain.Enums;

namespace SAM.Data.Configurations;

public class MonitoringWellConfiguration : IEntityTypeConfiguration<MonitoringWell>
{
    public void Configure(EntityTypeBuilder<MonitoringWell> builder)
    {
        builder.ToTable("MonitoringWells");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.WellId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.WellPermitNumber)
            .HasMaxLength(100);

        builder.Property(m => m.LocationDescription)
            .HasMaxLength(500);

        builder.Property(m => m.DiameterInches)
            .HasColumnType("decimal(18,2)");

        builder.Property(m => m.WellDepthFeet)
            .HasColumnType("decimal(18,2)");

        builder.Property(m => m.DepthToScreenFeet)
            .HasColumnType("decimal(18,2)");

        builder.Property(m => m.LowScreenDepthFeet)
            .HasColumnType("decimal(18,2)");

        builder.Property(m => m.HighScreenDepthFeet)
            .HasColumnType("decimal(18,2)");

        builder.Property(m => m.TopOfCasingElevationMsl)
            .HasColumnType("decimal(18,2)");

        builder.Property(m => m.TreatmentSystemLocation)
            .HasConversion<int>()
            .HasColumnType("int");

        builder.Property(m => m.Latitude)
            .HasColumnType("decimal(18,6)");

        builder.Property(m => m.Longitude)
            .HasColumnType("decimal(18,6)");

        builder.HasIndex(m => m.CompanyId);
        builder.HasIndex(m => new { m.CompanyId, m.WellId }).IsUnique();

        builder.HasOne(m => m.Company)
            .WithMany(c => c.MonitoringWells)
            .HasForeignKey(m => m.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


