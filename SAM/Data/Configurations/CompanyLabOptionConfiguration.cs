using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class CompanyLabOptionConfiguration : IEntityTypeConfiguration<CompanyLabOption>
{
    public void Configure(EntityTypeBuilder<CompanyLabOption> builder)
    {
        builder.ToTable("CompanyLabOptions");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CertificationNumber).HasMaxLength(100);
        builder.HasIndex(x => new { x.CompanyId, x.SortOrder });
    }
}
