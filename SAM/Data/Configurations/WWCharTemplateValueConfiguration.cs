using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class WWCharTemplateValueConfiguration : IEntityTypeConfiguration<WWCharTemplateValue>
{
    public void Configure(EntityTypeBuilder<WWCharTemplateValue> builder)
    {
        builder.ToTable("WWCharTemplateValues");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.NumericValue).HasPrecision(18, 6);
        builder.Property(x => x.TextValue).HasMaxLength(500);

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.WWCharId);
        builder.HasIndex(x => new { x.WWCharId, x.FacilityPermitTemplateParameterId, x.DayNo }).IsUnique();

        builder.HasOne(x => x.WWChar)
            .WithMany(w => w.TemplateValues)
            .HasForeignKey(x => x.WWCharId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FacilityPermitTemplateParameter)
            .WithMany(p => p.WWCharTemplateValues)
            .HasForeignKey(x => x.FacilityPermitTemplateParameterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
