using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class IrrRprtConfiguration : IEntityTypeConfiguration<IrrRprt>
{
    public void Configure(EntityTypeBuilder<IrrRprt> builder)
    {
        builder.ToTable("IrrRprts");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Month)
            .HasConversion<int>();

        builder.Property(i => i.Year)
            .IsRequired();

        builder.Property(i => i.TotalVolumeApplied)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.TotalApplicationRate)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.HydraulicLoadingRate)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.NitrogenLoadingRate)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.PanUptakeRate)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.ApplicationEfficiency)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.WeatherSummary)
            .HasMaxLength(1000);

        builder.Property(i => i.OperationalNotes)
            .HasMaxLength(2000);

        builder.Property(i => i.ComplianceStatus)
            .HasConversion<int>();

        builder.HasIndex(i => i.CompanyId);
        builder.HasIndex(i => i.FacilityId);
        builder.HasIndex(i => new { i.FacilityId, i.Month, i.Year }).IsUnique();

        builder.HasOne(i => i.Facility)
            .WithMany(f => f.IrrRprts)
            .HasForeignKey(i => i.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

