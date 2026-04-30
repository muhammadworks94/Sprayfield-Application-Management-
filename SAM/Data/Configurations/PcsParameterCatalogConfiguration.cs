using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;

namespace SAM.Data.Configurations;

public class PcsParameterCatalogConfiguration : IEntityTypeConfiguration<PcsParameterCatalog>
{
    public void Configure(EntityTypeBuilder<PcsParameterCatalog> builder)
    {
        builder.ToTable("PcsParameterCatalogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PcsCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.UserFriendlyName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.OfficialParameterName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.AcceptedUnits).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => x.PcsCode).IsUnique();
    }
}

