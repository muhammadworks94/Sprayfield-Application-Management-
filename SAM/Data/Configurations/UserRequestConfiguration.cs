using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class UserRequestConfiguration : IEntityTypeConfiguration<UserRequest>
{
    public void Configure(EntityTypeBuilder<UserRequest> builder)
    {
        builder.ToTable("UserRequests");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.CompanyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.AppRole)
            .HasConversion<int>();

        builder.Property(u => u.RequestedByEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.Status)
            .HasConversion<int>();

        builder.HasIndex(u => u.CompanyId);
        builder.HasIndex(u => u.Email);
        builder.HasIndex(u => u.Status);

        builder.HasOne(u => u.Company)
            .WithMany()
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

