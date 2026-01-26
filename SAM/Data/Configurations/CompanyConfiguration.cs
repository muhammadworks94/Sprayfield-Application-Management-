using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.ContactEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(c => c.ContactEmail);

        builder.Property(c => c.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Website)
            .HasMaxLength(500);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.TaxId)
            .HasMaxLength(50);

        builder.Property(c => c.FaxNumber)
            .HasMaxLength(20);

        builder.Property(c => c.LicenseNumber)
            .HasMaxLength(100);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.IsVerified)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(c => c.TaxId);
        builder.HasIndex(c => c.LicenseNumber);
    }
}


