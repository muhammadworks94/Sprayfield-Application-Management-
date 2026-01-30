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
    public async Task<IActionResult> Index(Guid? wellId = null)
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

        var viewModel = new AdvancedAnalyticsViewModel
        {
            ActiveTab = "well-trends",
            MonitoringWells = new SelectList(items, "Value", "Text", wellId?.ToString()),
            SelectedWellId = wellId
        };

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

        ViewData["Title"] = "Advanced Analytics";
        ViewData["PageSubtitle"] = "Analyze trends, compare parameters, and track historical data";
        ViewData["TitleIcon"] = "bar-chart-line";

        return View(viewModel);
    }
}
