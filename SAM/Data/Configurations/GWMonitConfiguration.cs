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

        builder.Property(g => g.GallonsPumped)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.Conductivity)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.TSS)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.NH3N)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.NO3N)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.TKN)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.Chloride)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.TOC)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.Calcium)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.Magnesium)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.FecalColiform)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.TotalColiform)
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.Odor)
            .HasMaxLength(100);

        builder.Property(g => g.Appearance)
            .HasMaxLength(100);

        builder.Property(g => g.VOCMethodNumber)
            .HasMaxLength(200);
        
        builder.Property(g => g.VOCReportFileStoragePath)
            .HasMaxLength(500);

        builder.Property(g => g.VOCReportFileName)
            .HasMaxLength(260);

        builder.Property(g => g.VOCReportContentType)
            .HasMaxLength(120);

        builder.Property(g => g.LabCertification)
            .HasMaxLength(500);

        builder.Property(g => g.CollectedBy)
            .HasMaxLength(200);

        builder.Property(g => g.AnalyzedBy)
            .HasMaxLength(200);

        builder.Property(g => g.LabSampleAnalyzedDate);

        builder.Property(g => g.Comments)
            .HasMaxLength(2000);

        builder.Property(g => g.GW59AQuestion2Details)
            .HasMaxLength(4000);

        builder.Property(g => g.GW59AQuestion4Details)
            .HasMaxLength(4000);

        builder.Property(g => g.GW59AQuestion5Details)
            .HasMaxLength(4000);

        builder.Property(g => g.GW59AQuestion7Details)
            .HasMaxLength(4000);

        builder.Property(g => g.GW59ASignerName)
            .HasMaxLength(200);

        builder.Property(g => g.GW59ASignerTitle)
            .HasMaxLength(200);

        builder.HasIndex(g => g.CompanyId);
        builder.HasIndex(g => g.FacilityId);
        builder.HasIndex(g => g.MonitoringWellId);
        builder.HasIndex(g => g.SampleDate);
        builder.HasIndex(g => g.LabOptionId);

        builder.HasOne(g => g.Facility)
            .WithMany(f => f.GWMonits)
            .HasForeignKey(g => g.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.MonitoringWell)
            .WithMany(m => m.GWMonits)
            .HasForeignKey(g => g.MonitoringWellId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.LabOption)
            .WithMany(l => l.GWMonits)
            .HasForeignKey(g => g.LabOptionId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}


