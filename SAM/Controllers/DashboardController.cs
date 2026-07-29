using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SAM.Controllers.Base;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Authorization;
using SAM.Services.Helpers;
using SAM.Services.Interfaces;
using SAM.Services.Models;
using SAM.ViewModels.Dashboard;

namespace SAM.Controllers;

/// <summary>
/// Controller for Dashboard - role-aware statistics and compliance indicators.
/// </summary>
[Authorize]
public class DashboardController : BaseController
{
    private const decimal WarningThresholdPercent = 75m;
    private const decimal CriticalThresholdPercent = 90m;

    private readonly IFacilityService _facilityService;
    private readonly ISprayfieldService _sprayfieldService;
    private readonly IMonitoringWellService _monitoringWellService;
    private readonly ICompanyService _companyService;
    private readonly IUserRequestService _userRequestService;
    private readonly IOperatorLogService _operatorLogService;
    private readonly IMonthlyApplicationService _monthlyApplicationService;
    private readonly IWWCharService _wwCharService;
    private readonly IGWMonitService _gwMonitService;
    private readonly IIrrRprtService _irrRprtService;
    private readonly IPermitAlertService _permitAlertService;
    private readonly IMonthlyLoadingResolutionService _monthlyLoadingResolution;

    public DashboardController(
        IFacilityService facilityService,
        ISprayfieldService sprayfieldService,
        IMonitoringWellService monitoringWellService,
        ICompanyService companyService,
        IUserRequestService userRequestService,
        IOperatorLogService operatorLogService,
        IMonthlyApplicationService monthlyApplicationService,
        IWWCharService wwCharService,
        IGWMonitService gwMonitService,
        IIrrRprtService irrRprtService,
        IPermitAlertService permitAlertService,
        IMonthlyLoadingResolutionService monthlyLoadingResolution,
        UserManager<ApplicationUser> userManager,
        ILogger<DashboardController> logger)
        : base(userManager, logger)
    {
        _facilityService = facilityService;
        _sprayfieldService = sprayfieldService;
        _monitoringWellService = monitoringWellService;
        _companyService = companyService;
        _userRequestService = userRequestService;
        _operatorLogService = operatorLogService;
        _monthlyApplicationService = monthlyApplicationService;
        _wwCharService = wwCharService;
        _gwMonitService = gwMonitService;
        _irrRprtService = irrRprtService;
        _permitAlertService = permitAlertService;
        _monthlyLoadingResolution = monthlyLoadingResolution;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var totalStopwatch = Stopwatch.StartNew();

        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        Guid? selectedCompanyId = null;
        if (isGlobalAdmin && Guid.TryParse(Request.Query["companyId"].FirstOrDefault(), out var parsedId))
        {
            selectedCompanyId = parsedId;
        }

        var companyId = selectedCompanyId ?? effectiveCompanyId;
        var isAdmin = User.IsInRole("admin");
        var isCompanyAdmin = User.IsInRole("company_admin");
        var isTechnician = User.IsInRole("technician");
        var isOperator = User.IsInRole("operator");

        var viewModel = new DashboardViewModel
        {
            IsGlobalAdmin = isGlobalAdmin,
            IsAdmin = isAdmin,
            IsCompanyAdmin = isCompanyAdmin,
            IsTechnician = isTechnician,
            IsOperator = isOperator,
            CanManageUsers = isAdmin || isCompanyAdmin,
            RoleViewLabel = isAdmin
                ? "Global Admin View"
                : isCompanyAdmin
                    ? "Company Admin View"
                    : isTechnician
                        ? "Technician View"
                        : isOperator
                            ? "Operator View"
                            : null
        };

        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        var dataLoadStopwatch = Stopwatch.StartNew();
        var facilities = (await _facilityService.GetAllAsync(companyId)).ToList();
        var sprayfields = (await _sprayfieldService.GetAllAsync(companyId)).ToList();
        var monitoringWells = (await _monitoringWellService.GetAllAsync(companyId)).ToList();
        var recentLogs = (await _operatorLogService.GetByDateRangeAsync(companyId, thirtyDaysAgo, now)).ToList();
        var recentIrrigations = (await _monthlyApplicationService.GetByDateRangeAsync(companyId, thirtyDaysAgo, now)).ToList();
        var wwCharRecords = await _wwCharService.GetDashboardRecordsAsync(companyId);
        var recentGWMonits = (await _gwMonitService.GetByDateRangeAsync(companyId, thirtyDaysAgo, now)).ToList();
        var reports = (await _irrRprtService.GetAllAsync(companyId)).ToList();
        var groundwaterOverview = await _gwMonitService.GetGroundwaterOverviewAsync(companyId);
        dataLoadStopwatch.Stop();
        Logger.LogDebug("Dashboard initial data load completed in {ElapsedMs}ms", dataLoadStopwatch.ElapsedMilliseconds);

        if (companyId.HasValue)
        {
            facilities = facilities.Where(f => f.CompanyId == companyId.Value).ToList();
            sprayfields = sprayfields.Where(s => s.CompanyId == companyId.Value).ToList();
            monitoringWells = monitoringWells.Where(w => w.CompanyId == companyId.Value).ToList();
            recentLogs = recentLogs.Where(l => l.CompanyId == companyId.Value).ToList();
            recentIrrigations = recentIrrigations.Where(a => a.CompanyId == companyId.Value).ToList();
            recentGWMonits = recentGWMonits.Where(g => g.CompanyId == companyId.Value).ToList();
            reports = reports.Where(r => r.CompanyId == companyId.Value).ToList();
        }

        viewModel.TotalFacilities = facilities.Count;
        viewModel.TotalSprayfields = sprayfields.Count;
        viewModel.TotalSprayfieldAcres = sprayfields.Sum(s => s.SizeAcres);
        viewModel.TotalMonitoringWells = monitoringWells.Count;

        if (isGlobalAdmin)
        {
            var companies = await _companyService.GetAllAsync();
            if (effectiveCompanyId.HasValue)
            {
                companies = companies.Where(c => c.Id == effectiveCompanyId.Value);
            }

            viewModel.TotalCompanies = companies.Count();
        }

        if (isGlobalAdmin)
        {
            viewModel.PendingUserRequests = (await _userRequestService.GetPendingRequestsAsync()).Count();
        }
        else if (effectiveCompanyId.HasValue)
        {
            viewModel.PendingUserRequests = (await _userRequestService.GetPendingRequestsAsync(effectiveCompanyId.Value)).Count();
        }

        viewModel.RecentOperatorLogs = recentLogs.Count;
        viewModel.RecentIrrigations = recentIrrigations.Count;
        viewModel.RecentActivityCount = recentIrrigations.OrderByDescending(i => i.CreatedDate).Take(10).Count();
        viewModel.RecentWWCharRecords = wwCharRecords.Count(r => r.CreatedDate >= thirtyDaysAgo);
        viewModel.RecentGWMonitRecords = recentGWMonits.Count;

        var recentReports = reports.Where(r => r.CreatedDate >= thirtyDaysAgo).ToList();
        viewModel.CompliantReports = recentReports.Count(r => r.ComplianceStatus == ComplianceStatusEnum.Compliant);
        viewModel.NonCompliantReports = recentReports.Count(r => r.ComplianceStatus == ComplianceStatusEnum.NonCompliant);
        viewModel.UnderReviewReports = recentReports.Count(r => r.ComplianceStatus == ComplianceStatusEnum.UnderReview);

        var totalReports = viewModel.CompliantReports + viewModel.UnderReviewReports + viewModel.NonCompliantReports;
        viewModel.IrrigationCompliance.CompliantCount = viewModel.CompliantReports;
        viewModel.IrrigationCompliance.CompliantPercent = totalReports > 0 ? (viewModel.CompliantReports * 100.0 / totalReports) : 0;
        viewModel.IrrigationCompliance.UnderReviewCount = viewModel.UnderReviewReports;
        viewModel.IrrigationCompliance.UnderReviewPercent = totalReports > 0 ? (viewModel.UnderReviewReports * 100.0 / totalReports) : 0;
        viewModel.IrrigationCompliance.NonCompliantCount = viewModel.NonCompliantReports;
        viewModel.IrrigationCompliance.NonCompliantPercent = totalReports > 0 ? (viewModel.NonCompliantReports * 100.0 / totalReports) : 0;

        viewModel.SystemUptimePercent = recentIrrigations.Count > 0 ? 100.0 : 0.0;
        var efficiencyDates = recentIrrigations
            .OrderByDescending(i => i.CreatedDate)
            .Take(5)
            .Select(i => i.CreatedDate.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .Take(2)
            .ToList();

        foreach (var d in efficiencyDates)
        {
            viewModel.OperationalEfficiencyByDate.Add(new OperationalEfficiencyPointViewModel
            {
                DateLabel = d.ToString("dd/MM/yyyy"),
                Value = 1.0,
                Status = "Normal"
            });
        }

        var wastewaterByPeriod = wwCharRecords
            .GroupBy(w => new { w.Month, w.Year })
            .OrderByDescending(g => g.Key.Year)
            .ThenByDescending(g => g.Key.Month)
            .Take(6)
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month);

        foreach (var g in wastewaterByPeriod)
        {
            decimal? avgBod5 = null;
            decimal? avgTss = null;
            var bod5Values = g.SelectMany(w => w.BOD5Daily ?? new List<decimal?>()).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            var tssValues = g.SelectMany(w => w.TSSDaily ?? new List<decimal?>()).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            if (bod5Values.Count > 0) avgBod5 = (decimal)bod5Values.Average();
            if (tssValues.Count > 0) avgTss = (decimal)tssValues.Average();
            viewModel.WastewaterTrends.Add(new WastewaterTrendPointViewModel
            {
                PeriodLabel = $"{(MonthEnum)g.Key.Month} {g.Key.Year}",
                AvgBOD5 = avgBod5,
                AvgTSS = avgTss
            });
        }

        viewModel.GroundwaterOverview.AvgPH = groundwaterOverview.AvgPH;
        viewModel.GroundwaterOverview.AvgConductivity = groundwaterOverview.AvgConductivity;

        foreach (var well in monitoringWells)
        {
            viewModel.GroundwaterOverview.ByWell.Add(new GroundwaterWellPointViewModel
            {
                WellId = well.WellId,
                PH = groundwaterOverview.LatestPhByWellId.TryGetValue(well.Id, out var lastPh) ? lastPh : null
            });
        }
        viewModel.GroundwaterOverview.ByWell = viewModel.GroundwaterOverview.ByWell.OrderBy(w => w.WellId).ToList();

        var facilityMap = facilities.ToDictionary(f => f.Id, f => f.Name);

        var activities = new List<RecentActivityViewModel>();
        foreach (var log in recentLogs.Take(5))
        {
            activities.Add(new RecentActivityViewModel { Type = "Operator Log", Description = $"Operator log by {log.CreatedBy}", Date = log.CreatedDate, User = log.CreatedBy });
        }
        foreach (var irrigation in recentIrrigations.Take(5))
        {
            activities.Add(new RecentActivityViewModel { Type = "Application", Description = "Zone application logged", Date = irrigation.CreatedDate, User = irrigation.CreatedBy });
        }
        foreach (var report in recentReports.Take(5))
        {
            activities.Add(new RecentActivityViewModel
            {
                Type = "Report",
                Description = $"Report for {(facilityMap.TryGetValue(report.FacilityId, out var reportFacilityName) ? reportFacilityName : "Facility")} - {report.Month} {report.Year}",
                Date = report.CreatedDate,
                User = report.CreatedBy
            });
        }
        viewModel.RecentActivities = activities.OrderByDescending(a => a.Date).Take(5).ToList();

        var needsComplianceAttention = viewModel.NonCompliantReports > 0 || viewModel.UnderReviewReports > 0;
        var needsMonitoringAttention = viewModel.RecentGWMonitRecords == 0 && viewModel.TotalMonitoringWells > 0;
        viewModel.SystemStatuses = new List<SystemStatusViewModel>
        {
            new() { SystemName = "Wastewater System", Status = "Normal", IsNormalOrOperational = true },
            new() { SystemName = "Irrigation System", Status = "Operational", IsNormalOrOperational = true },
            new() { SystemName = "Monitoring System", Status = needsMonitoringAttention ? "Attention Required" : "Normal", IsNormalOrOperational = !needsMonitoringAttention },
            new() { SystemName = "Compliance Status", Status = needsComplianceAttention ? "Attention Required" : "Compliant", IsNormalOrOperational = !needsComplianceAttention }
        };

        var complianceSummaries = new List<ComplianceSummaryViewModel>();
        foreach (var report in recentReports.OrderByDescending(r => r.CreatedDate).Take(10))
        {
            complianceSummaries.Add(new ComplianceSummaryViewModel
            {
                FacilityName = facilityMap.TryGetValue(report.FacilityId, out var facilityName) ? facilityName : "Unknown",
                Period = $"{report.Month} {report.Year}",
                ComplianceStatus = report.ComplianceStatus.ToString(),
                ReportDate = report.CreatedDate
            });
        }
        viewModel.ComplianceSummaries = complianceSummaries;

        var loadingOffset = 0;
        if (int.TryParse(Request.Query["loadingOffset"].FirstOrDefault(), out var parsedOffset))
        {
            loadingOffset = Math.Clamp(parsedOffset, 0, 3);
        }

        var asOfMonthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-loadingOffset);
        var rollingAsOfDate = new DateTime(
            asOfMonthStart.Year,
            asOfMonthStart.Month,
            DateTime.DaysInMonth(asOfMonthStart.Year, asOfMonthStart.Month));

        viewModel.LoadingOffset = loadingOffset;
        viewModel.IsRealTimeLoadingPeriod = loadingOffset == 0;
        viewModel.LoadingPeriodOptions = BuildLoadingPeriodOptions(now);
        viewModel.SelectedLoadingPeriodLabel = viewModel.LoadingPeriodOptions
            .First(o => o.Offset == loadingOffset).Label;
        viewModel.CurrentMonthLoadingColumnHeader = loadingOffset == 0
            ? "Current Month Loading (in)"
            : $"{asOfMonthStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture)} Loading (in)";

        var visibleFacilityIds = facilities.Select(f => f.Id).ToHashSet();
        sprayfields = sprayfields.Where(s => s.FacilityId.HasValue && visibleFacilityIds.Contains(s.FacilityId.Value)).ToList();
        var facilityNameById = facilities.ToDictionary(f => f.Id, f => f.Name);

        var fieldLoadingStopwatch = Stopwatch.StartNew();
        var orderedSprayfields = sprayfields.OrderBy(s => s.FacilityId).ThenBy(s => s.FieldId).ToList();
        var fieldLoadingMetrics = await _monthlyLoadingResolution.GetBatchFieldLoadingMetricsAsync(
            orderedSprayfields,
            rollingAsOfDate);
        fieldLoadingStopwatch.Stop();
        Logger.LogDebug(
            "Dashboard field loading batch completed for {SprayfieldCount} sprayfields in {ElapsedMs}ms",
            orderedSprayfields.Count,
            fieldLoadingStopwatch.ElapsedMilliseconds);

        foreach (var sf in orderedSprayfields)
        {
            if (!sf.FacilityId.HasValue)
            {
                continue;
            }

            var metrics = fieldLoadingMetrics.TryGetValue(sf.Id, out var loadingMetrics)
                ? loadingMetrics
                : new SprayfieldLoadingMetrics();

            var annualLimit = sf.AnnualRateInches ?? sf.HydraulicLoadingLimitInPerYr;
            var currentMonthUtilPct = annualLimit > 0m ? (metrics.CurrentMonthInches / annualLimit) * 100m : 0m;
            var rolling12UtilPct = annualLimit > 0m ? (metrics.Rolling12MonthInches / annualLimit) * 100m : 0m;
            var annualUtilPct = rolling12UtilPct;

            viewModel.FieldLoadingProgress.Add(new FieldLoadingProgressViewModel
            {
                FacilityId = sf.FacilityId.Value,
                FacilityName = facilityNameById.TryGetValue(sf.FacilityId.Value, out var facilityName)
                    ? facilityName
                    : "Unknown Facility",
                SprayfieldId = sf.Id,
                FieldCode = sf.FieldId,
                AnnualLimitInches = annualLimit,
                CurrentMonthLoadingInches = metrics.CurrentMonthInches,
                Rolling12MonthLoadingInches = metrics.Rolling12MonthInches,
                CurrentMonthUtilizationPercent = currentMonthUtilPct,
                Rolling12MonthUtilizationPercent = rolling12UtilPct,
                CurrentMonthStatus = ToThresholdStatus(currentMonthUtilPct),
                Rolling12MonthStatus = ToThresholdStatus(rolling12UtilPct),
                AnnualStatus = ToThresholdStatus(annualUtilPct)
            });
        }

        viewModel.FieldLoadingProgress = viewModel.FieldLoadingProgress
            .OrderBy(x => StatusSortRank(x.AnnualStatus))
            .ThenBy(x => x.FacilityName)
            .ThenBy(x => x.FieldCode, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (loadingOffset > 0)
        {
            viewModel.IncompleteHistoryWarning = await BuildIncompleteHistoryWarningAsync(
                orderedSprayfields,
                asOfMonthStart,
                companyId);
        }

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        ViewBag.SelectedCompanyId = companyId;

        var companyName = "All Companies";
        if (companyId.HasValue)
        {
            var company = await _companyService.GetByIdAsync(companyId.Value);
            companyName = company?.Name ?? "Company";
        }

        viewModel.SelectedCompanyName = companyName;

        var permitAlerts = await _permitAlertService.GetAlertsAsync(companyId, now);
        viewModel.PermitAlerts = permitAlerts.Select(a => new PermitAlertViewModel
        {
            FacilityName = a.FacilityName,
            PermitNumber = a.PermitNumber,
            PermitVersion = a.PermitVersion,
            Message = a.Message,
            DaysUntilExpiration = a.DaysUntilExpiration,
            Severity = a.Severity.ToString()
        }).ToList();

        ViewData["TitleIcon"] = "speedometer2";
        ViewData["PageSubtitle"] = "Environmental Monitoring Overview";

        totalStopwatch.Stop();
        Logger.LogDebug("Dashboard Index completed in {ElapsedMs}ms", totalStopwatch.ElapsedMilliseconds);

        return View(viewModel);
    }

    private static string ToThresholdStatus(decimal utilizationPercent)
    {
        if (utilizationPercent >= CriticalThresholdPercent)
        {
            return "Critical";
        }

        if (utilizationPercent >= WarningThresholdPercent)
        {
            return "Warning";
        }

        return "Normal";
    }

    private static int StatusSortRank(string status) => status switch
    {
        "Critical" => 0,
        "Warning" => 1,
        _ => 2
    };

    private static List<LoadingPeriodOptionViewModel> BuildLoadingPeriodOptions(DateTime now)
    {
        var options = new List<LoadingPeriodOptionViewModel>
        {
            new() { Offset = 0, Label = "Real-Time" }
        };

        var currentMonthStart = new DateTime(now.Year, now.Month, 1);
        for (var offset = 1; offset <= 3; offset++)
        {
            var month = currentMonthStart.AddMonths(-offset);
            options.Add(new LoadingPeriodOptionViewModel
            {
                Offset = offset,
                Label = month.ToString("MMMM yyyy", CultureInfo.InvariantCulture)
            });
        }

        return options;
    }

    private async Task<string?> BuildIncompleteHistoryWarningAsync(
        IReadOnlyList<Sprayfield> sprayfields,
        DateTime asOfMonthStart,
        Guid? scopedCompanyId)
    {
        var companyIds = scopedCompanyId.HasValue
            ? new List<Guid> { scopedCompanyId.Value }
            : sprayfields.Select(s => s.CompanyId).Distinct().ToList();

        if (companyIds.Count == 0)
        {
            return null;
        }

        var includeCompanyNames = companyIds.Count > 1;
        var segments = new List<string>();

        foreach (var id in companyIds)
        {
            var company = await _companyService.GetByIdAsync(id);
            if (company?.FirstReportingMonth is not int firstMonth
                || company.FirstReportingYear is not int firstYear
                || firstMonth < 1
                || firstMonth > 12)
            {
                continue;
            }

            var missingMonths = GetMonthsBeforeCoverage(asOfMonthStart, firstYear, firstMonth);
            if (missingMonths.Count == 0)
            {
                continue;
            }

            var monthList = string.Join(", ", missingMonths);
            segments.Add(includeCompanyNames
                ? $"{company.Name}: {monthList}"
                : monthList);
        }

        if (segments.Count == 0)
        {
            return null;
        }

        var detail = string.Join("; ", segments);
        var noun = includeCompanyNames ? "these clients'" : "this client's";
        return $"Incomplete 12-month history: {detail} are outside {noun} covered period.";
    }

    private static List<string> GetMonthsBeforeCoverage(
        DateTime asOfMonthStart,
        int firstReportingYear,
        int firstReportingMonth)
    {
        var coverageStart = new DateTime(firstReportingYear, firstReportingMonth, 1)
            .AddMonths(-BaselineWindowHelper.HistoricalMonthCount);
        var coverageStartKey = coverageStart.Year * 12 + coverageStart.Month;

        var missing = new List<string>();
        for (var i = 11; i >= 0; i--)
        {
            var monthDate = asOfMonthStart.AddMonths(-i);
            var monthKey = monthDate.Year * 12 + monthDate.Month;
            if (monthKey < coverageStartKey)
            {
                missing.Add(monthDate.ToString("MMMM yyyy", CultureInfo.InvariantCulture));
            }
        }

        return missing;
    }
}
