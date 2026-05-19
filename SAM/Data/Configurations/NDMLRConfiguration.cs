using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class NDMLRConfiguration : IEntityTypeConfiguration<NDMLR>
{
    public void Configure(EntityTypeBuilder<NDMLR> builder)
    {
        builder.ToTable("NDMLRs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Year)
            .IsRequired();

        builder.Property(x => x.SourceNotes)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.FacilityId);
        builder.HasIndex(x => new { x.FacilityId, x.Year }).IsUnique();

        builder.HasOne(x => x.Facility)
            .WithMany(f => f.NDMLRs)
            .HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
