using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class CompanyRequestConfiguration : IEntityTypeConfiguration<CompanyRequest>
{
    public void Configure(EntityTypeBuilder<CompanyRequest> builder)
    {
        builder.ToTable("CompanyRequests");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CompanyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.ContactEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.PhoneNumber)
            .HasMaxLength(50);

        builder.Property(c => c.Website)
            .HasMaxLength(500);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.TaxId)
            .HasMaxLength(50);

        builder.Property(c => c.FaxNumber)
            .HasMaxLength(50);

        builder.Property(c => c.LicenseNumber)
            .HasMaxLength(100);

        builder.Property(c => c.RequesterFullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.RequesterEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.Status)
            .HasConversion<int>();

        builder.Property(c => c.RejectionReason)
            .HasMaxLength(1000);

        builder.HasIndex(c => c.RequesterEmail);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.CreatedCompanyId);

        builder.HasOne(c => c.CreatedCompany)
            .WithMany()
            .HasForeignKey(c => c.CreatedCompanyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

