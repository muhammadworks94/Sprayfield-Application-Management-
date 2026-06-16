using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class LoadCalculationConfiguration : IEntityTypeConfiguration<LoadCalculation>
{
    public void Configure(EntityTypeBuilder<LoadCalculation> builder)
    {
        builder.ToTable("LoadCalculations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.LbsApplied).HasColumnType("decimal(18,6)");
        builder.Property(c => c.LbsPerAcre).HasColumnType("decimal(18,6)");
        builder.Property(c => c.ZoneAcresSnapshot).HasColumnType("decimal(18,4)");
        builder.Property(c => c.ZonePercentSnapshot).HasColumnType("decimal(9,4)");
        builder.Property(c => c.FormulaVersion).HasMaxLength(32);

        builder.HasIndex(c => c.ApplicationId);
        builder.HasIndex(c => c.CalculatedAtUtc);

        builder.HasOne(c => c.Application)
            .WithMany(a => a.LoadCalculations)
            .HasForeignKey(c => c.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
