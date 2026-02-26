using Microsoft.AspNetCore.Mvc.Rendering;

namespace SAM.ViewModels.Analytics;

/// <summary>
/// View model for the Advanced Analytics page with tabs (Well Trends, Parameters, Facilities, Historical).
/// </summary>
public class AdvancedAnalyticsViewModel
{
    public string ActiveTab { get; set; } = "well-trends";

    // Well Trends tab
    public SelectList? MonitoringWells { get; set; }
    public Guid? SelectedWellId { get; set; }

    /// <summary>
    /// Whether the selected well has any trend data to display.
    /// </summary>
    public bool HasWellTrendData { get; set; }

    /// <summary>
    /// Date labels for well trend chart (e.g. "Dec 2025").
    /// </summary>
    public List<string> WellTrendDateLabels { get; set; } = new();

    /// <summary>
    /// pH values per sample date for the selected well.
    /// </summary>
    public List<decimal> WellTrendPH { get; set; } = new();

    /// <summary>
    /// Conductivity values (µS/cm) per sample date for the selected well.
    /// </summary>
    public List<decimal> WellTrendConductivity { get; set; } = new();

    // Parameters tab - cross-well comparison for a single parameter
    public SelectList? ParameterOptions { get; set; }
    public string? SelectedParameterKey { get; set; }
    public bool HasParameterData { get; set; }
    public List<string> ParameterChartLabels { get; set; } = new();
    public List<decimal> ParameterChartValues { get; set; } = new();

    // Facilities tab - aggregated metrics per facility
    public List<FacilityAnalyticsItem> FacilityAnalytics { get; set; } = new();

    /// <summary>
    /// Labels for the facility risk chart (typically top facilities by risk).
    /// </summary>
    public List<string> FacilityRiskLabels { get; set; } = new();

    /// <summary>
    /// Risk values (e.g. max of pH/cond out-of-range percentages) for the facility risk chart.
    /// </summary>
    public List<decimal> FacilityRiskValues { get; set; } = new();

    // Historical tab - company-wide time series
    public bool HasHistoricalData { get; set; }
    public List<string> HistoricalDateLabels { get; set; } = new();
    public List<decimal> HistoricalAvgPH { get; set; } = new();
    public List<decimal> HistoricalAvgConductivity { get; set; } = new();
}

/// <summary>
/// Aggregated groundwater metrics per facility for the Facilities tab.
/// </summary>
public class FacilityAnalyticsItem
{
    public string FacilityName { get; set; } = string.Empty;
    public int MonitoringWellCount { get; set; }
    public int SampleCount { get; set; }
    public decimal? AvgPH { get; set; }
    public decimal? AvgConductivity { get; set; }

    /// <summary>
    /// Facility status bucket based on groundwater quality: Normal, Watch, Alert, or No Data.
    /// </summary>
    public string Status { get; set; } = "No Data";

    /// <summary>
    /// Percentage of recent samples with pH outside the acceptable range.
    /// </summary>
    public decimal OutOfRangePhPercent { get; set; }

    /// <summary>
    /// Percentage of recent samples with conductivity above the alert threshold.
    /// </summary>
    public decimal HighConductivityPercent { get; set; }

    /// <summary>
    /// Most recent sample date for this facility.
    /// </summary>
    public DateTime? LastSampleDate { get; set; }

    /// <summary>
    /// Composite risk score (higher means more concern), used for sorting and charts.
    /// </summary>
    public decimal RiskScore { get; set; }
}
