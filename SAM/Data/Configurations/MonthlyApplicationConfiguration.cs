using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class MonthlyApplicationConfiguration : IEntityTypeConfiguration<MonthlyApplication>
{
    public void Configure(EntityTypeBuilder<MonthlyApplication> builder)
    {
        builder.ToTable("MonthlyApplications");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ApplicationDate).IsRequired();
        builder.Property(a => a.VolumeGallons).HasColumnType("decimal(18,2)");
        builder.Property(a => a.TimeIrrigatedMinutes).HasColumnType("decimal(10,2)");
        builder.Property(a => a.MaximumHourlyLoadingInchesPerAcre).HasColumnType("decimal(18,2)");
        builder.Property(a => a.OperatorSnapshotName).HasMaxLength(200);
        builder.Property(a => a.Comments).HasMaxLength(2000);

        builder.HasIndex(a => a.CompanyId);
        builder.HasIndex(a => a.FacilityId);
        builder.HasIndex(a => a.SprayfieldId);
        builder.HasIndex(a => a.ApplicationDate);
        builder.HasIndex(a => a.OperatorUserId);

        builder.HasOne(a => a.Company)
            .WithMany()
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Facility)
            .WithMany(f => f.MonthlyApplications)
            .HasForeignKey(a => a.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Sprayfield)
            .WithMany(s => s.MonthlyApplications)
            .HasForeignKey(a => a.SprayfieldId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.OperatorUser)
            .WithMany()
            .HasForeignKey(a => a.OperatorUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
