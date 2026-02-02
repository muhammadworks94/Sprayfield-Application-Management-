using Microsoft.AspNetCore.Mvc.Rendering;

namespace SAM.ViewModels.Analytics;

/// <summary>
/// View model for the Advanced Analytics page with tabs (Well Trends, Parameters, Facilities, Historical).
/// </summary>
public class AdvancedAnalyticsViewModel
{
    public string ActiveTab { get; set; } = "well-trends";

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
}
