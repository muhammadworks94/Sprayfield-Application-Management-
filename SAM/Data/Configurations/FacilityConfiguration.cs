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

        builder.HasIndex(f => f.CompanyId);
        builder.HasIndex(f => f.PermitNumber);

        builder.HasOne(f => f.Company)
            .WithMany(c => c.Facilities)
            .HasForeignKey(f => f.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


