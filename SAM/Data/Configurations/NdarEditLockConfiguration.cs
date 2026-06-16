using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class NdarEditLockConfiguration : IEntityTypeConfiguration<NdarEditLock>
{
    public void Configure(EntityTypeBuilder<NdarEditLock> builder)
    {
        builder.ToTable("NdarEditLocks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LockedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(x => x.LockedByDisplayName)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(x => new { x.FacilityId, x.EditDate })
            .IsUnique();

        builder.HasIndex(x => x.ExpiresAtUtc);

        builder.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Facility)
            .WithMany()
            .HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.LockedByUser)
            .WithMany()
            .HasForeignKey(x => x.LockedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

