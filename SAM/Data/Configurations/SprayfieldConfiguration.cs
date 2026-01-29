using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class SprayfieldConfiguration : IEntityTypeConfiguration<Sprayfield>
{
    public void Configure(EntityTypeBuilder<Sprayfield> builder)
    {
        builder.ToTable("Sprayfields");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.FieldId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.SizeAcres)
            .HasColumnType("decimal(18,2)");

        builder.Property(s => s.HydraulicLoadingLimitInPerYr)
            .HasColumnType("decimal(18,2)");

        builder.Property(s => s.HourlyRateInches)
            .HasColumnType("decimal(18,2)");

        builder.Property(s => s.AnnualRateInches)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(s => s.CompanyId);
        builder.HasIndex(s => new { s.CompanyId, s.FieldId }).IsUnique();

        builder.HasOne(s => s.Company)
            .WithMany(c => c.Sprayfields)
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Soil)
            .WithMany(so => so.Sprayfields)
            .HasForeignKey(s => s.SoilId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Crop)
            .WithMany(cr => cr.Sprayfields)
            .HasForeignKey(s => s.CropId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Nozzle)
            .WithMany(n => n.Sprayfields)
            .HasForeignKey(s => s.NozzleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Facility)
            .WithMany(f => f.Sprayfields)
            .HasForeignKey(s => s.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


