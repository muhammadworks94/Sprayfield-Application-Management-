using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAM.Domain.Entities;
using System.Text.Json;

namespace SAM.Data.Configurations;

public class NDAR1Configuration : IEntityTypeConfiguration<NDAR1>
{
    public void Configure(EntityTypeBuilder<NDAR1> builder)
    {
        builder.ToTable("NDAR1s");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Month)
            .HasConversion<int>();

        builder.Property(n => n.Year)
            .IsRequired();

        // Configure JSON columns for daily arrays
        var jsonOptions = new JsonSerializerOptions { WriteIndented = false };

        // Weather daily arrays
        builder.Property(n => n.WeatherCodeDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<string?>>(v, jsonOptions) ?? new List<string?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(n => n.TemperatureDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(n => n.PrecipitationDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(n => n.StorageDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(n => n.FiveDayUpsetDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        // Field 1 daily arrays
        builder.Property(n => n.Field1VolumeAppliedDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(n => n.Field1TimeIrrigatedDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(n => n.Field1DailyLoadingDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(n => n.Field1MaxHourlyLoadingDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        // Field 2 daily arrays
        builder.Property(n => n.Field2VolumeAppliedDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(n => n.Field2TimeIrrigatedDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(n => n.Field2DailyLoadingDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(n => n.Field2MaxHourlyLoadingDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        // Field 3 daily arrays
        builder.Property(n => n.Field3VolumeAppliedDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(n => n.Field3TimeIrrigatedDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(n => n.Field3DailyLoadingDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(n => n.Field3MaxHourlyLoadingDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        // Field 4 daily arrays
        builder.Property(n => n.Field4VolumeAppliedDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(n => n.Field4TimeIrrigatedDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(n => n.Field4DailyLoadingDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        builder.Property(n => n.Field4MaxHourlyLoadingDaily)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<decimal?>>(v, jsonOptions) ?? new List<decimal?>())
            .HasColumnType("nvarchar(max)");

        // Monthly totals and 12-month floating totals
        builder.Property(n => n.Field1MonthlyLoading)
            .HasColumnType("decimal(18,2)");

        builder.Property(n => n.Field1MaxHourlyLoading)
            .HasColumnType("decimal(18,2)");

        builder.Property(n => n.Field1TwelveMonthFloatingTotal)
            .HasColumnType("decimal(18,2)");

        builder.Property(n => n.Field2MonthlyLoading)
            .HasColumnType("decimal(18,2)");

        builder.Property(n => n.Field2MaxHourlyLoading)
            .HasColumnType("decimal(18,2)");

        builder.Property(n => n.Field2TwelveMonthFloatingTotal)
            .HasColumnType("decimal(18,2)");

        builder.Property(n => n.Field3MonthlyLoading)
            .HasColumnType("decimal(18,2)");

        builder.Property(n => n.Field3MaxHourlyLoading)
            .HasColumnType("decimal(18,2)");

        builder.Property(n => n.Field3TwelveMonthFloatingTotal)
            .HasColumnType("decimal(18,2)");

        builder.Property(n => n.Field4MonthlyLoading)
            .HasColumnType("decimal(18,2)");

        builder.Property(n => n.Field4MaxHourlyLoading)
            .HasColumnType("decimal(18,2)");

        builder.Property(n => n.Field4TwelveMonthFloatingTotal)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(n => n.CompanyId);
        builder.HasIndex(n => n.FacilityId);
        builder.HasIndex(n => new { n.FacilityId, n.Month, n.Year }).IsUnique();

        builder.HasOne(n => n.Facility)
            .WithMany(f => f.NDAR1s)
            .HasForeignKey(n => n.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Field1)
            .WithMany()
            .HasForeignKey(n => n.Field1Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Field2)
            .WithMany()
            .HasForeignKey(n => n.Field2Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Field3)
            .WithMany()
            .HasForeignKey(n => n.Field3Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Field4)
            .WithMany()
            .HasForeignKey(n => n.Field4Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

