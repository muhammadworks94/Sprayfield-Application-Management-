using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class SprayfieldBaselineMonthlyLoadingConfiguration : IEntityTypeConfiguration<SprayfieldBaselineMonthlyLoading>
{
    public void Configure(EntityTypeBuilder<SprayfieldBaselineMonthlyLoading> builder)
    {
        builder.ToTable("SprayfieldBaselineMonthlyLoadings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Year).IsRequired();
        builder.Property(x => x.Month).IsRequired();
        builder.Property(x => x.LoadingInches).HasColumnType("decimal(18,6)");

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.FacilityId);
        builder.HasIndex(x => new { x.FacilityId, x.SprayfieldId, x.Year, x.Month }).IsUnique();

        builder.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Facility)
            .WithMany()
            .HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Sprayfield)
            .WithMany()
            .HasForeignKey(x => x.SprayfieldId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
