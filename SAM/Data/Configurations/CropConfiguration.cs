using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class CropConfiguration : IEntityTypeConfiguration<Crop>
{
    public void Configure(EntityTypeBuilder<Crop> builder)
    {
        builder.ToTable("Crops");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.PanFactor)
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.NUptake)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(c => c.CompanyId);

        builder.HasOne(c => c.Company)
            .WithMany(co => co.Crops)
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


