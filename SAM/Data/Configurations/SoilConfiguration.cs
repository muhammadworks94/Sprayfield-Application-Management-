using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class SoilConfiguration : IEntityTypeConfiguration<Soil>
{
    public void Configure(EntityTypeBuilder<Soil> builder)
    {
        builder.ToTable("Soils");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TypeName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.Property(s => s.Permeability)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(s => s.CompanyId);

        builder.HasOne(s => s.Company)
            .WithMany(c => c.Soils)
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

