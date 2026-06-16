using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class FacilityPermitConfiguration : IEntityTypeConfiguration<FacilityPermit>
{
    public void Configure(EntityTypeBuilder<FacilityPermit> builder)
    {
        builder.ToTable("FacilityPermits");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PermitNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PermitVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.GwOperationLagoon).HasDefaultValue(true);
        builder.Property(x => x.GwOperationSprayField).HasDefaultValue(true);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.PermitPdfFileName).HasMaxLength(260);
        builder.Property(x => x.PermitPdfStoragePath).HasMaxLength(500);

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.FacilityId);
        builder.HasIndex(x => new { x.FacilityId, x.EffectiveStartDate, x.EffectiveEndDate });
        builder.HasIndex(x => new { x.FacilityId, x.PermitNumber, x.PermitVersion }).IsUnique();

        builder.HasOne(x => x.Facility)
            .WithMany(f => f.FacilityPermits)
            .HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
