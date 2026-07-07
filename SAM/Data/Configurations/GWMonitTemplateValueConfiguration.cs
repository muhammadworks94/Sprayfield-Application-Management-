using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class GWMonitTemplateValueConfiguration : IEntityTypeConfiguration<GWMonitTemplateValue>
{
    public void Configure(EntityTypeBuilder<GWMonitTemplateValue> builder)
    {
        builder.ToTable("GWMonitTemplateValues");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.NumericValue).HasPrecision(18, 6);
        builder.Property(x => x.TextValue).HasMaxLength(500);
        builder.Property(x => x.IsReportingDetectionLimit).HasDefaultValue(false);

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.GWMonitId);
        builder.HasIndex(x => new { x.GWMonitId, x.FacilityPermitTemplateParameterId }).IsUnique();

        builder.HasOne(x => x.GWMonit)
            .WithMany(g => g.TemplateValues)
            .HasForeignKey(x => x.GWMonitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FacilityPermitTemplateParameter)
            .WithMany(p => p.GWMonitTemplateValues)
            .HasForeignKey(x => x.FacilityPermitTemplateParameterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
