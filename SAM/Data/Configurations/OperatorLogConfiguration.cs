using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class OperatorLogConfiguration : IEntityTypeConfiguration<OperatorLog>
{
    public void Configure(EntityTypeBuilder<OperatorLog> builder)
    {
        builder.ToTable("OperatorLogs");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.LogDate)
            .IsRequired();

        builder.Property(o => o.OperatorName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.Shift)
            .HasConversion<int>();

        builder.Property(o => o.WeatherConditions)
            .HasMaxLength(200);

        builder.Property(o => o.SystemStatus)
            .HasConversion<int>();

        builder.Property(o => o.MaintenancePerformed)
            .HasMaxLength(2000);

        builder.Property(o => o.EquipmentInspected)
            .HasMaxLength(2000);

        builder.Property(o => o.IssuesNoted)
            .HasMaxLength(2000);

        builder.Property(o => o.CorrectiveActions)
            .HasMaxLength(2000);

        builder.Property(o => o.NextShiftNotes)
            .HasMaxLength(2000);

        builder.HasIndex(o => o.CompanyId);
        builder.HasIndex(o => o.FacilityId);
        builder.HasIndex(o => o.LogDate);

        builder.HasOne(o => o.Facility)
            .WithMany(f => f.OperatorLogs)
            .HasForeignKey(o => o.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


