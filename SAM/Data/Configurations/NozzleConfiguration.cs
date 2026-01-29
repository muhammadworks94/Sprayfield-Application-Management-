using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class NozzleConfiguration : IEntityTypeConfiguration<Nozzle>
{
    public void Configure(EntityTypeBuilder<Nozzle> builder)
    {
        builder.ToTable("Nozzles");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Model)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(n => n.Manufacturer)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.FlowRateGpm)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(n => n.CompanyId);

        builder.HasOne(n => n.Company)
            .WithMany(c => c.Nozzles)
            .HasForeignKey(n => n.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


