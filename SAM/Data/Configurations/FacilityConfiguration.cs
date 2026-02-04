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

        builder.Property(f => f.PermitNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.Permittee)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.FacilityClass)
            .HasMaxLength(100);

        builder.Property(f => f.Address)
            .HasMaxLength(500);

        builder.Property(f => f.City)
            .HasMaxLength(100);

        builder.Property(f => f.State)
            .HasMaxLength(50);

        builder.Property(f => f.ZipCode)
            .HasMaxLength(20);

        builder.Property(f => f.County)
            .HasMaxLength(100);

        // Optional regulatory metadata fields
        builder.Property(f => f.PermitPhone)
            .HasMaxLength(50);

        builder.Property(f => f.FacilityPhone)
            .HasMaxLength(50);

        builder.Property(f => f.OrcName)
            .HasMaxLength(200);

        builder.Property(f => f.OperatorGrade)
            .HasMaxLength(50);

        builder.Property(f => f.OperatorNumber)
            .HasMaxLength(50);

        builder.Property(f => f.CertifiedLaboratory1Name)
            .HasMaxLength(200);

        builder.Property(f => f.CertifiedLaboratory2Name)
            .HasMaxLength(200);

        builder.Property(f => f.LabCertificationNumber1)
            .HasMaxLength(100);

        builder.Property(f => f.LabCertificationNumber2)
            .HasMaxLength(100);

        builder.Property(f => f.PersonsCollectingSamples)
            .HasMaxLength(200);

        builder.HasIndex(f => f.CompanyId);
        builder.HasIndex(f => f.PermitNumber);

        builder.HasOne(f => f.Company)
            .WithMany(c => c.Facilities)
            .HasForeignKey(f => f.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


