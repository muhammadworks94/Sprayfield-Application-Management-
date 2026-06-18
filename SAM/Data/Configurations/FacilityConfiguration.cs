using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class FacilityConfiguration : IEntityTypeConfiguration<Facility>
{
    public void Configure(EntityTypeBuilder<Facility> builder)
    {
        builder.ToTable("Facilities");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.Permittee)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.FacilityClass)
            .HasMaxLength(100);

        builder.Property(f => f.PermitPhone)
            .HasMaxLength(50);

        builder.Property(f => f.FacilityPhone)
            .HasMaxLength(50);

        builder.Property(f => f.FacilityContactPerson)
            .HasMaxLength(200);

        builder.Property(f => f.FacilityContactPersonPhone)
            .HasMaxLength(50);

        builder.Property(f => f.OrcName)
            .HasMaxLength(200);

        builder.Property(f => f.OperatorGrade)
            .HasMaxLength(50);

        builder.Property(f => f.OperatorNumber)
            .HasMaxLength(50);

        builder.Property(f => f.OperatorPhone)
            .HasMaxLength(50);

        builder.Property(f => f.PersonsCollectingSamples)
            .HasMaxLength(200);

        builder.Property(f => f.MineralizationRatePercent)
            .HasPrecision(5, 2);

        builder.Property(f => f.VolatilizationRatePercent)
            .HasPrecision(5, 2);

        builder.HasOne(f => f.DefaultLabOption)
            .WithMany(l => l.Facilities)
            .HasForeignKey(f => f.DefaultLabOptionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(f => f.DefaultFacilityPermit)
            .WithMany()
            .HasForeignKey(f => f.DefaultFacilityPermitId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(f => f.CompanyId);

        builder.HasOne(f => f.Company)
            .WithMany(c => c.Facilities)
            .HasForeignKey(f => f.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

