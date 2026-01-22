using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class IrrigateConfiguration : IEntityTypeConfiguration<Irrigate>
{
    public void Configure(EntityTypeBuilder<Irrigate> builder)
    {
        builder.ToTable("Irrigates");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.IrrigationDate)
            .IsRequired();

        builder.Property(i => i.DurationHours)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.FlowRateGpm)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.TotalVolumeGallons)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.ApplicationRateInches)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.WindSpeed)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.WindDirection)
            .HasMaxLength(50);

        builder.Property(i => i.WeatherConditions)
            .HasMaxLength(200);

        builder.Property(i => i.Operator)
            .HasMaxLength(200);

        builder.Property(i => i.Comments)
            .HasMaxLength(2000);

        builder.HasIndex(i => i.CompanyId);
        builder.HasIndex(i => i.FacilityId);
        builder.HasIndex(i => i.SprayfieldId);
        builder.HasIndex(i => i.IrrigationDate);

        builder.HasOne(i => i.Facility)
            .WithMany(f => f.Irrigates)
            .HasForeignKey(i => i.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Sprayfield)
            .WithMany(s => s.Irrigates)
            .HasForeignKey(i => i.SprayfieldId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

