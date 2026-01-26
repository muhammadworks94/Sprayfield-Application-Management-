using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class GWMonitConfiguration : IEntityTypeConfiguration<GWMonit>
{
    public void Configure(EntityTypeBuilder<GWMonit> builder)
    {
        builder.ToTable("GWMonits");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.SampleDate)
            .IsRequired();

        builder.Property(g => g.SampleDepth)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.WaterLevel)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.Temperature)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.PH)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.Conductivity)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.TDS)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.Turbidity)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.BOD5)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.COD)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.TSS)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.NH3N)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.NO3N)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.TKN)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.TotalPhosphorus)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.Chloride)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.FecalColiform)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.TotalColiform)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.LabCertification)
            .HasMaxLength(500);

        builder.Property(g => g.CollectedBy)
            .HasMaxLength(200);

        builder.Property(g => g.AnalyzedBy)
            .HasMaxLength(200);

        builder.Property(g => g.Comments)
            .HasMaxLength(2000);

        builder.HasIndex(g => g.CompanyId);
        builder.HasIndex(g => g.FacilityId);
        builder.HasIndex(g => g.MonitoringWellId);
        builder.HasIndex(g => g.SampleDate);

        builder.HasOne(g => g.Facility)
            .WithMany(f => f.GWMonits)
            .HasForeignKey(g => g.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.MonitoringWell)
            .WithMany(m => m.GWMonits)
            .HasForeignKey(g => g.MonitoringWellId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


