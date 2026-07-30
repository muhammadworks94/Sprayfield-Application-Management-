using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class FacilityOrcAssignmentConfiguration : IEntityTypeConfiguration<FacilityOrcAssignment>
{
    public void Configure(EntityTypeBuilder<FacilityOrcAssignment> builder)
    {
        builder.ToTable("FacilityOrcAssignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).HasMaxLength(450);

        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.OperatorNumber).HasMaxLength(50);
        builder.Property(x => x.OperatorGrade).HasMaxLength(50);
        builder.Property(x => x.OperatorPhone).HasMaxLength(50);

        builder.Property(x => x.StartDate).HasColumnType("date");
        builder.Property(x => x.EndDate).HasColumnType("date");

        builder.HasOne(x => x.Facility)
            .WithMany(f => f.OrcAssignments)
            .HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.FacilityId);
        builder.HasIndex(x => new { x.FacilityId, x.EndDate });
    }
}
