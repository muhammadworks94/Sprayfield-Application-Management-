using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Controllers.Base;
using SAM.Domain.Entities;
using SAM.Services.Interfaces;
using SAM.ViewModels.Analytics;

namespace SAM.Controllers;

/// <summary>
/// Controller for Advanced Analytics page with tabs (Well Trends, Parameters, Facilities, Historical).
/// </summary>
[Authorize]
public class AnalyticsController : BaseController
{
    private readonly IMonitoringWellService _monitoringWellService;
    private readonly IGWMonitService _gwMonitService;

    public AnalyticsController(
        IMonitoringWellService monitoringWellService,
        IGWMonitService gwMonitService,
        UserManager<ApplicationUser> userManager,
        ILogger<AnalyticsController> logger)
        : base(userManager, logger)
    {
        _monitoringWellService = monitoringWellService;
        _gwMonitService = gwMonitService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? wellId = null, string? activeTab = null, string? parameterKey = null)
    {
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();
        var companyId = effectiveCompanyId;

        var wells = await _monitoringWellService.GetAllAsync(companyId);
        var wellList = wells.OrderBy(w => w.WellId).ToList();

        var items = wellList
            .Select(w => new SelectListItem
            {
                Value = w.Id.ToString(),
                Text = $"{w.WellId} - {w.LocationDescription}",
                Selected = wellId.HasValue && w.Id == wellId.Value
            })
            .ToList();

        items.Insert(0, new SelectListItem { Value = "", Text = "-- Select Monitoring Well --", Selected = !wellId.HasValue });

        var normalizedActiveTab = string.IsNullOrWhiteSpace(activeTab) ? "well-trends" : activeTab;

        var viewModel = new AdvancedAnalyticsViewModel
        {
            ActiveTab = normalizedActiveTab,
            MonitoringWells = new SelectList(items, "Value", "Text", wellId?.ToString()),
            SelectedWellId = wellId
        };

        // Well Trends tab - per-well pH and conductivity over time
        if (wellId.HasValue)
        {
            var gwReadings = (await _gwMonitService.GetAllAsync(companyId, null, wellId))
                .OrderBy(r => r.SampleDate)
                .ToList();
            viewModel.HasWellTrendData = gwReadings.Any();
            if (viewModel.HasWellTrendData)
            {
                viewModel.WellTrendDateLabels = gwReadings.Select(r => r.SampleDate.ToString("MMM yyyy")).ToList();
                viewModel.WellTrendPH = gwReadings.Select(r => r.PH ?? 0m).ToList();
                viewModel.WellTrendConductivity = gwReadings.Select(r => r.Conductivity ?? 0m).ToList();
            }
        }

        // Load all groundwater data once for additional analytics tabs
        var gwAll = (await _gwMonitService.GetAllAsync(companyId)).ToList();

        // Parameters tab - cross-well comparison
        var parameterItems = new List<SelectListItem>
        {
            new() { Value = "ph", Text = "pH" },
            new() { Value = "cond", Text = "Conductivity (µS/cm)" },
            new() { Value = "nh3n", Text = "Ammonia-N (NH3-N)" },
            new() { Value = "no3n", Text = "Nitrate-N (NO3-N)" }
        };
        var selectedParameterKey = string.IsNullOrWhiteSpace(parameterKey) ? "ph" : parameterKey;
        viewModel.SelectedParameterKey = selectedParameterKey;
        viewModel.ParameterOptions = new SelectList(parameterItems, "Value", "Text", selectedParameterKey);

        if (gwAll.Any())
        {
            decimal? Selector(SAM.Domain.Entities.GWMonit g) => selectedParameterKey switch
            {
                "ph" => g.PH,
                "cond" => g.Conductivity,
                "nh3n" => g.NH3N,
                "no3n" => g.NO3N,
                _ => g.PH
            };

            var gwByWell = gwAll
                .GroupBy(g => g.MonitoringWellId)
                .ToList();

            var wellsById = wellList.ToDictionary(w => w.Id, w => w);

            foreach (var group in gwByWell)
            {
                if (!wellsById.TryGetValue(group.Key, out var well))
                    continue;

                // Use the most recent non-null value for the selected parameter
                var latestWithValue = group
                    .OrderByDescending(g => g.SampleDate)
                    .FirstOrDefault(g => Selector(g).HasValue);

                var value = latestWithValue != null ? Selector(latestWithValue) : null;
                if (value.HasValue)
                {
                    viewModel.ParameterChartLabels.Add(well.WellId);
                    viewModel.ParameterChartValues.Add(value.Value);
                }
            }

            viewModel.HasParameterData = viewModel.ParameterChartLabels.Count > 0;
        }

        // Facilities tab - aggregated metrics per facility
        if (gwAll.Any())
        {
            const decimal MinPh = 6.5m;
            const decimal MaxPh = 8.5m;
            const decimal ConductivityAlertThreshold = 1000m; // µS/cm
            var cutoff = DateTime.UtcNow.AddMonths(-12);

            var groupedByFacility = gwAll
                .GroupBy(g => new
                {
                    g.FacilityId,
                    FacilityName = g.Facility != null ? g.Facility.Name : "Unknown facility"
                });

            foreach (var group in groupedByFacility)
            {
                var facilitySamples = group.ToList();
                var wellIds = facilitySamples.Select(g => g.MonitoringWellId).Distinct().ToList();

                var phValues = facilitySamples.Where(g => g.PH.HasValue).Select(g => g.PH!.Value).ToList();
                var condValues = facilitySamples.Where(g => g.Conductivity.HasValue).Select(g => g.Conductivity!.Value).ToList();

                decimal? avgPh = phValues.Count > 0 ? (decimal)phValues.Average() : null;
                decimal? avgCond = condValues.Count > 0 ? (decimal)condValues.Average() : null;

                // Use recent window for status metrics; fall back to all if none
                var recent = facilitySamples.Where(g => g.SampleDate >= cutoff).ToList();
                if (!recent.Any())
                {
                    recent = facilitySamples;
                }

                var total = recent.Count;
                var outOfRangePh = recent.Count(g => g.PH.HasValue && (g.PH.Value < MinPh || g.PH.Value > MaxPh));
                var highCond = recent.Count(g => g.Conductivity.HasValue && g.Conductivity.Value > ConductivityAlertThreshold);

                var outOfRangePhPercent = total > 0 ? (decimal)outOfRangePh * 100m / total : 0m;
                var highCondPercent = total > 0 ? (decimal)highCond * 100m / total : 0m;

                var lastSampleDate = recent.OrderByDescending(g => g.SampleDate).FirstOrDefault()?.SampleDate;

                string status;
                if (total == 0)
                {
                    status = "No Data";
                }
                else if (outOfRangePhPercent >= 30m || highCondPercent >= 30m)
                {
                    status = "Alert";
                }
                else if (outOfRangePhPercent >= 10m || highCondPercent >= 10m)
                {
                    status = "Watch";
                }
                else
                {
                    status = "Normal";
                }

                var riskScore = Math.Max(outOfRangePhPercent, highCondPercent);

                viewModel.FacilityAnalytics.Add(new FacilityAnalyticsItem
                {
                    FacilityName = group.Key.FacilityName,
                    MonitoringWellCount = wellIds.Count,
                    SampleCount = facilitySamples.Count,
                    AvgPH = avgPh,
                    AvgConductivity = avgCond,
                    OutOfRangePhPercent = outOfRangePhPercent,
                    HighConductivityPercent = highCondPercent,
                    LastSampleDate = lastSampleDate,
                    Status = status,
                    RiskScore = riskScore
                });
            }

            viewModel.FacilityAnalytics = viewModel.FacilityAnalytics
                .OrderByDescending(f => f.RiskScore)
                .ThenByDescending(f => f.SampleCount)
                .ThenBy(f => f.FacilityName)
                .ToList();

            // Build top facilities list for risk chart (skip zero-risk facilities)
            var topRiskFacilities = viewModel.FacilityAnalytics
                .Where(f => f.RiskScore > 0)
                .Take(5)
                .ToList();

            viewModel.FacilityRiskLabels = topRiskFacilities.Select(f => f.FacilityName).ToList();
            viewModel.FacilityRiskValues = topRiskFacilities.Select(f => f.RiskScore).ToList();
        }

        // Historical tab - monthly company-wide averages
        if (gwAll.Any())
        {
            var groupedByMonth = gwAll
                .GroupBy(g => new { g.SampleDate.Year, g.SampleDate.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .ToList();

            foreach (var group in groupedByMonth)
            {
                var phValues = group.Where(g => g.PH.HasValue).Select(g => g.PH!.Value).ToList();
                var condValues = group.Where(g => g.Conductivity.HasValue).Select(g => g.Conductivity!.Value).ToList();

                if (phValues.Count == 0 && condValues.Count == 0)
                    continue;

                var label = new DateTime(group.Key.Year, group.Key.Month, 1).ToString("MMM yyyy");
                viewModel.HistoricalDateLabels.Add(label);
                viewModel.HistoricalAvgPH.Add(phValues.Count > 0 ? (decimal)phValues.Average() : 0m);
                viewModel.HistoricalAvgConductivity.Add(condValues.Count > 0 ? (decimal)condValues.Average() : 0m);
            }

            viewModel.HasHistoricalData = viewModel.HistoricalDateLabels.Count > 0;
        }

        ViewData["Title"] = "Advanced Analytics";
        ViewData["PageSubtitle"] = "Analyze trends, compare parameters, and track historical data";
        ViewData["TitleIcon"] = "bar-chart-line";

        return View(viewModel);
    }
}
