using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SAM.Controllers.Base;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Authorization;
using SAM.Services.Interfaces;
using SAM.ViewModels.Dashboard;

namespace SAM.Controllers;

/// <summary>
/// Controller for Dashboard - role-aware statistics and compliance indicators.
/// </summary>
[Authorize]
public class DashboardController : BaseController
{
    private readonly IFacilityService _facilityService;
    private readonly ISprayfieldService _sprayfieldService;
    private readonly IMonitoringWellService _monitoringWellService;
    private readonly ICompanyService _companyService;
    private readonly IUserRequestService _userRequestService;
    private readonly IOperatorLogService _operatorLogService;
    private readonly IIrrigateService _irrigateService;
    private readonly IWWCharService _wwCharService;
    private readonly IGWMonitService _gwMonitService;
    private readonly IIrrRprtService _irrRprtService;

    public DashboardController(
        IFacilityService facilityService,
        ISprayfieldService sprayfieldService,
        IMonitoringWellService monitoringWellService,
        ICompanyService companyService,
        IUserRequestService userRequestService,
        IOperatorLogService operatorLogService,
        IIrrigateService irrigateService,
        IWWCharService wwCharService,
        IGWMonitService gwMonitService,
        IIrrRprtService irrRprtService,
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
        _irrigateService = irrigateService;
        _wwCharService = wwCharService;
        _gwMonitService = gwMonitService;
        _irrRprtService = irrRprtService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
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

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var facilities = (await _facilityService.GetAllAsync(companyId)).ToList();
        var sprayfields = (await _sprayfieldService.GetAllAsync(companyId)).ToList();
        var monitoringWells = (await _monitoringWellService.GetAllAsync(companyId)).ToList();
        var recentLogs = (await _operatorLogService.GetByDateRangeAsync(companyId, thirtyDaysAgo, DateTime.UtcNow)).ToList();
        var recentIrrigations = (await _irrigateService.GetByDateRangeAsync(companyId, thirtyDaysAgo, DateTime.UtcNow)).ToList();
        var allWWChars = (await _wwCharService.GetAllAsync(companyId)).ToList();
        var recentGWMonits = (await _gwMonitService.GetByDateRangeAsync(companyId, thirtyDaysAgo, DateTime.UtcNow)).ToList();
        var reports = (await _irrRprtService.GetAllAsync(companyId)).ToList();

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
        viewModel.RecentWWCharRecords = allWWChars.Count(r => r.CreatedDate >= thirtyDaysAgo);
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

        var wastewaterByPeriod = allWWChars
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

        var gwList = (await _gwMonitService.GetAllAsync(companyId)).ToList();
        var phValues = gwList.Where(g => g.PH.HasValue).Select(g => g.PH!.Value).ToList();
        var condValues = gwList.Where(g => g.Conductivity.HasValue).Select(g => g.Conductivity!.Value).ToList();
        viewModel.GroundwaterOverview.AvgPH = phValues.Count > 0 ? (decimal)phValues.Average() : null;
        viewModel.GroundwaterOverview.AvgConductivity = condValues.Count > 0 ? (decimal)condValues.Average() : null;

        var latestPhByWell = gwList
            .Where(g => g.PH.HasValue)
            .GroupBy(g => g.MonitoringWellId)
            .Select(g => g.OrderByDescending(x => x.SampleDate).First())
            .ToDictionary(x => x.MonitoringWellId, x => x.PH);

        foreach (var well in monitoringWells)
        {
            viewModel.GroundwaterOverview.ByWell.Add(new GroundwaterWellPointViewModel
            {
                WellId = well.WellId,
                PH = latestPhByWell.TryGetValue(well.Id, out var lastPh) ? lastPh : null
            });
        }
        viewModel.GroundwaterOverview.ByWell = viewModel.GroundwaterOverview.ByWell.OrderBy(w => w.WellId).ToList();

        var activities = new List<RecentActivityViewModel>();
        foreach (var log in recentLogs.Take(5))
        {
            activities.Add(new RecentActivityViewModel { Type = "Operator Log", Description = $"Operator log by {log.CreatedBy}", Date = log.CreatedDate, User = log.CreatedBy });
        }
        foreach (var irrigation in recentIrrigations.Take(5))
        {
            activities.Add(new RecentActivityViewModel { Type = "Irrigation", Description = $"Irrigation completed on field {irrigation.Sprayfield?.FieldId ?? irrigation.SprayfieldId.ToString("N")}", Date = irrigation.CreatedDate, User = irrigation.CreatedBy });
        }
        foreach (var report in recentReports.Take(5))
        {
            activities.Add(new RecentActivityViewModel { Type = "Report", Description = $"Report for {report.Facility?.Name ?? "Facility"} - {report.Month} {report.Year}", Date = report.CreatedDate, User = report.CreatedBy });
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

        var facilityMap = facilities.ToDictionary(f => f.Id, f => f.Name);
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

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        ViewBag.SelectedCompanyId = selectedCompanyId;

        var companyName = "All Companies";
        if (companyId.HasValue)
        {
            var company = await _companyService.GetByIdAsync(companyId.Value);
            companyName = company?.Name ?? "Company";
        }

        viewModel.SelectedCompanyName = companyName;
        ViewData["TitleIcon"] = "speedometer2";
        ViewData["PageSubtitle"] = $"{companyName} - Environmental Monitoring Overview";

        return View(viewModel);
    }
}
