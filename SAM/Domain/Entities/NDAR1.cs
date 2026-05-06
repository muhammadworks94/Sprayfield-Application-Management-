using SAM.Domain.Entities.Base;
using SAM.Domain.Enums;

namespace SAM.Domain.Entities;

/// <summary>
/// Records monthly Non-Discharge Application Report (NDAR-1) data for a facility.
/// Daily arrays are stored as JSON columns.
/// </summary>
public class NDAR1 : CompanyScopedEntity
{
    /// <summary>
    /// Reference to the Facility entity.
    /// </summary>
    public Guid FacilityId { get; set; }

    /// <summary>
    /// Month of the report.
    /// </summary>
    public MonthEnum Month { get; set; }

    /// <summary>
    /// Year of the report.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Did irrigation occur at this facility?
    /// </summary>
    public bool DidIrrigationOccur { get; set; }

    /// <summary>
    /// Array of daily weather codes (max 31 items, stored as JSON).
    /// </summary>
    public List<string?> WeatherCodeDaily { get; set; } = new List<string?>();

    /// <summary>
    /// Array of daily temperature values in °F (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> TemperatureDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily precipitation values in inches (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> PrecipitationDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily 5-Day Upset values in feet (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> FiveDayUpsetDaily { get; set; } = new List<decimal?>();

    // Field 1
    /// <summary>
    /// Reference to Field 1 Sprayfield entity.
    /// </summary>
    public Guid? Field1Id { get; set; }

    /// <summary>
    /// Array of daily volume applied for Field 1 in gallons (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> Field1VolumeAppliedDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily time irrigated for Field 1 in minutes (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> Field1TimeIrrigatedDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily loading for Field 1 in inches (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> Field1DailyLoadingDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily maximum hourly loading for Field 1 in inches (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> Field1MaxHourlyLoadingDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Monthly loading total for Field 1 in inches.
    /// </summary>
    public decimal Field1MonthlyLoading { get; set; }

    /// <summary>
    /// Maximum hourly loading for Field 1 in inches.
    /// </summary>
    public decimal Field1MaxHourlyLoading { get; set; }

    /// <summary>
    /// 12-month floating total for Field 1 in inches.
    /// </summary>
    public decimal Field1TwelveMonthFloatingTotal { get; set; }

    // Field 2
    /// <summary>
    /// Reference to Field 2 Sprayfield entity.
    /// </summary>
    public Guid? Field2Id { get; set; }

    /// <summary>
    /// Array of daily volume applied for Field 2 in gallons (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> Field2VolumeAppliedDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily time irrigated for Field 2 in minutes (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> Field2TimeIrrigatedDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily loading for Field 2 in inches (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> Field2DailyLoadingDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily maximum hourly loading for Field 2 in inches (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> Field2MaxHourlyLoadingDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Monthly loading total for Field 2 in inches.
    /// </summary>
    public decimal Field2MonthlyLoading { get; set; }

    /// <summary>
    /// Maximum hourly loading for Field 2 in inches.
    /// </summary>
    public decimal Field2MaxHourlyLoading { get; set; }

    /// <summary>
    /// 12-month floating total for Field 2 in inches.
    /// </summary>
    public decimal Field2TwelveMonthFloatingTotal { get; set; }

    // Field 3
    /// <summary>
    /// Reference to Field 3 Sprayfield entity.
    /// </summary>
    public Guid? Field3Id { get; set; }

    /// <summary>
    /// Array of daily volume applied for Field 3 in gallons (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> Field3VolumeAppliedDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily time irrigated for Field 3 in minutes (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> Field3TimeIrrigatedDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily loading for Field 3 in inches (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> Field3DailyLoadingDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily maximum hourly loading for Field 3 in inches (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> Field3MaxHourlyLoadingDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Monthly loading total for Field 3 in inches.
    /// </summary>
    public decimal Field3MonthlyLoading { get; set; }

    /// <summary>
    /// Maximum hourly loading for Field 3 in inches.
    /// </summary>
    public decimal Field3MaxHourlyLoading { get; set; }

    /// <summary>
    /// 12-month floating total for Field 3 in inches.
    /// </summary>
    public decimal Field3TwelveMonthFloatingTotal { get; set; }

    // Field 4
    /// <summary>
    /// Reference to Field 4 Sprayfield entity.
    /// </summary>
    public Guid? Field4Id { get; set; }

    /// <summary>
    /// Array of daily volume applied for Field 4 in gallons (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> Field4VolumeAppliedDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily time irrigated for Field 4 in minutes (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> Field4TimeIrrigatedDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily loading for Field 4 in inches (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> Field4DailyLoadingDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily maximum hourly loading for Field 4 in inches (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> Field4MaxHourlyLoadingDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Monthly loading total for Field 4 in inches.
    /// </summary>
    public decimal Field4MonthlyLoading { get; set; }

    /// <summary>
    /// Maximum hourly loading for Field 4 in inches.
    /// </summary>
    public decimal Field4MaxHourlyLoading { get; set; }

    /// <summary>
    /// 12-month floating total for Field 4 in inches.
    /// </summary>
    public decimal Field4TwelveMonthFloatingTotal { get; set; }

    // Navigation properties
    public Facility? Facility { get; set; }
    public Sprayfield? Field1 { get; set; }
    public Sprayfield? Field2 { get; set; }
    public Sprayfield? Field3 { get; set; }
    public Sprayfield? Field4 { get; set; }
    public ICollection<NDAR1Field> Fields { get; set; } = new List<NDAR1Field>();
}

/// <summary>
/// Dynamic per-sprayfield NDAR-1 data for a report month.
/// </summary>
public class NDAR1Field
{
    public Guid Id { get; set; }
    public Guid NDAR1Id { get; set; }
    public Guid SprayfieldId { get; set; }
    public int FieldOrder { get; set; }

    public decimal MonthlyLoading { get; set; }
    public decimal MaxHourlyLoading { get; set; }
    public decimal TwelveMonthFloatingTotal { get; set; }

    public NDAR1? NDAR1 { get; set; }
    public Sprayfield? Sprayfield { get; set; }
    public ICollection<NDAR1FieldDaily> DailyValues { get; set; } = new List<NDAR1FieldDaily>();
}

/// <summary>
/// Daily values for one NDAR field row (1..31).
/// </summary>
public class NDAR1FieldDaily
{
    public Guid Id { get; set; }
    public Guid NDAR1FieldId { get; set; }
    public int DayNo { get; set; }
    public decimal? VolumeApplied { get; set; }
    public decimal? TimeIrrigated { get; set; }
    public decimal? DailyLoading { get; set; }
    public decimal? MaxHourlyLoading { get; set; }

    public NDAR1Field? NDAR1Field { get; set; }
}

