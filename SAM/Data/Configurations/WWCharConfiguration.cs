using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using System.Text.Json;

namespace SAM.Data.Configurations;

public class WWCharConfiguration : IEntityTypeConfiguration<WWChar>
{
    public void Configure(EntityTypeBuilder<WWChar> builder)
    {
        builder.ToTable("WWChars");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Month)
            .HasConversion<int>();

        builder.Property(w => w.Year)
            .IsRequired();

        // Configure JSON columns for daily arrays
        var jsonOptions = new JsonSerializerOptions { WriteIndented = false };

        var bod5DailyProperty = builder.Property(w => w.BOD5Daily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>());
        
        bod5DailyProperty.Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<decimal?>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c != null ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())) : 0,
            c => c != null ? c.ToList() : new List<decimal?>()));
        
        bod5DailyProperty.HasColumnType("nvarchar(max)");

        builder.Property(w => w.TSSDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(w => w.FlowRateDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(w => w.PHDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(w => w.NH3NDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(w => w.FecalColiformDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(w => w.TotalColiformDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(w => w.ChlorideDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(w => w.TDSDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(w => w.CompositeTime)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<string?>>(v, jsonOptions) ?? new List<string?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(w => w.ORCOnSite)
            .HasConversion(new ORCOnSiteConverter())
            .HasColumnType("nvarchar(max)");

        builder.Property(w => w.LagoonFreeboard)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(w => w.LabCertification)
            .HasMaxLength(500);

        builder.Property(w => w.CollectedBy)
            .HasMaxLength(200);

        builder.Property(w => w.AnalyzedBy)
            .HasMaxLength(200);

        builder.HasIndex(w => w.CompanyId);
        builder.HasIndex(w => w.FacilityId);
        builder.HasIndex(w => new { w.FacilityId, w.Month, w.Year }).IsUnique();

        builder.HasOne(w => w.Facility)
            .WithMany(f => f.WWChars)
            .HasForeignKey(w => w.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

