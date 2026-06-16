using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class NDAR1FieldDailyConfiguration : IEntityTypeConfiguration<NDAR1FieldDaily>
{
    public void Configure(EntityTypeBuilder<NDAR1FieldDaily> builder)
    {
        builder.ToTable("NDAR1FieldDailies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.VolumeApplied).HasColumnType("decimal(18,6)");
        builder.Property(x => x.TimeIrrigated).HasColumnType("decimal(18,6)");
        builder.Property(x => x.DailyLoading).HasColumnType("decimal(18,6)");
        builder.Property(x => x.MaxHourlyLoading).HasColumnType("decimal(18,6)");

        builder.HasIndex(x => new { x.NDAR1FieldId, x.DayNo }).IsUnique();

        builder.HasOne(x => x.NDAR1Field)
            .WithMany(x => x.DailyValues)
            .HasForeignKey(x => x.NDAR1FieldId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

