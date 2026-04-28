using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class NDAR1FieldConfiguration : IEntityTypeConfiguration<NDAR1Field>
{
    public void Configure(EntityTypeBuilder<NDAR1Field> builder)
    {
        builder.ToTable("NDAR1Fields");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MonthlyLoading).HasColumnType("decimal(18,6)");
        builder.Property(x => x.MaxHourlyLoading).HasColumnType("decimal(18,6)");
        builder.Property(x => x.TwelveMonthFloatingTotal).HasColumnType("decimal(18,6)");

        builder.HasIndex(x => new { x.NDAR1Id, x.FieldOrder }).IsUnique();
        builder.HasIndex(x => new { x.NDAR1Id, x.SprayfieldId }).IsUnique();

        builder.HasOne(x => x.NDAR1)
            .WithMany(x => x.Fields)
            .HasForeignKey(x => x.NDAR1Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Sprayfield)
            .WithMany()
            .HasForeignKey(x => x.SprayfieldId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

