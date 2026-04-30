using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;
using SAM.Domain.Enums;

namespace SAM.Data.Configurations;

public class FacilityPermitTemplateParameterConfiguration : IEntityTypeConfiguration<FacilityPermitTemplateParameter>
{
    public void Configure(EntityTypeBuilder<FacilityPermitTemplateParameter> builder)
    {
        builder.ToTable("FacilityPermitTemplateParameters");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ParameterDisplayOverride).HasMaxLength(255);
        builder.Property(x => x.UnitsOverride).HasMaxLength(100);
        builder.Property(x => x.ScheduledMonthsCsv).HasMaxLength(100);
        builder.Property(x => x.MonthlyAverageLimit).HasPrecision(18, 6);
        builder.Property(x => x.MonthlyGeometricMeanLimit).HasPrecision(18, 6);
        builder.Property(x => x.DailyMinimumLimit).HasPrecision(18, 6);
        builder.Property(x => x.DailyMaximumLimit).HasPrecision(18, 6);
        builder.Property(x => x.SampleType).HasConversion<int>();
        builder.Property(x => x.MeasurementFrequency).HasConversion<int>();
        builder.Property(x => x.ReportTypes).HasConversion<int>().HasDefaultValue(PermitTemplateReportTypeEnum.Ndmr);

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.FacilityPermitId);
        builder.HasIndex(x => new { x.FacilityPermitId, x.SortOrder });
        builder.HasIndex(x => new { x.FacilityPermitId, x.PcsParameterCatalogId }).IsUnique();

        builder.HasOne(x => x.FacilityPermit)
            .WithMany(p => p.TemplateParameters)
            .HasForeignKey(x => x.FacilityPermitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PcsParameterCatalog)
            .WithMany(c => c.PermitTemplateParameters)
            .HasForeignKey(x => x.PcsParameterCatalogId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
