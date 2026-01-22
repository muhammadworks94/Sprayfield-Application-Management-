using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAM.Controllers.Base;
using SAM.Data;
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
    private readonly ApplicationDbContext _context;
    private readonly IFacilityService _facilityService;
    private readonly ISprayfieldService _sprayfieldService;
    private readonly IMonitoringWellService _monitoringWellService;
    private readonly ICompanyService _companyService;
    private readonly IUserRequestService _userRequestService;
    private readonly IAdminRequestService _adminRequestService;
    private readonly IOperatorLogService _operatorLogService;
    private readonly IIrrigateService _irrigateService;
    private readonly IWWCharService _wwCharService;
    private readonly IGWMonitService _gwMonitService;
    private readonly IIrrRprtService _irrRprtService;

    public DashboardController(
        ApplicationDbContext context,
        IFacilityService facilityService,
        ISprayfieldService sprayfieldService,
        IMonitoringWellService monitoringWellService,
        ICompanyService companyService,
        IUserRequestService userRequestService,
        IAdminRequestService adminRequestService,
        IOperatorLogService operatorLogService,
        IIrrigateService irrigateService,
        IWWCharService wwCharService,
        IGWMonitService gwMonitService,
        IIrrRprtService irrRprtService,
        UserManager<ApplicationUser> userManager,
        ILogger<DashboardController> logger)
        : base(userManager, logger)
    {
        _context = context;
        _facilityService = facilityService;
        _sprayfieldService = sprayfieldService;
        _monitoringWellService = monitoringWellService;
        _companyService = companyService;
        _userRequestService = userRequestService;
        _adminRequestService = adminRequestService;
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

        var viewModel = new DashboardViewModel();

        // Get company-scoped data
        Guid? companyId = isGlobalAdmin ? null : effectiveCompanyId;

        // Count facilities
        var facilities = await _facilityService.GetAllAsync(companyId);
        viewModel.TotalFacilities = facilities.Count();

        // Count sprayfields
        var sprayfields = await _sprayfieldService.GetAllAsync(companyId);
        viewModel.TotalSprayfields = sprayfields.Count();

        // Count monitoring wells
        var monitoringWells = await _monitoringWellService.GetAllAsync(companyId);
        viewModel.TotalMonitoringWells = monitoringWells.Count();

        // Count companies (only for global admins)
        if (isGlobalAdmin)
        {
            var companies = await _companyService.GetAllAsync();
            viewModel.TotalCompanies = companies.Count();
        }

        // Count pending requests
        if (isGlobalAdmin)
        {
            var pendingUserRequests = await _userRequestService.GetPendingRequestsAsync();
            viewModel.PendingUserRequests = pendingUserRequests.Count();

            var pendingAdminRequests = await _adminRequestService.GetPendingRequestsAsync();
            viewModel.PendingAdminRequests = pendingAdminRequests.Count();
        }
        else if (effectiveCompanyId.HasValue)
        {
            var pendingUserRequests = await _userRequestService.GetPendingRequestsAsync(effectiveCompanyId.Value);
            viewModel.PendingUserRequests = pendingUserRequests.Count();
        }

        // Get recent activity (last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        
        // Recent operator logs
        var recentLogs = await _operatorLogService.GetByDateRangeAsync(companyId, thirtyDaysAgo, DateTime.UtcNow);
        viewModel.RecentOperatorLogs = recentLogs.Count();

        // Recent irrigations
        var recentIrrigations = await _irrigateService.GetByDateRangeAsync(companyId, thirtyDaysAgo, DateTime.UtcNow);
        viewModel.RecentIrrigations = recentIrrigations.Count();

        // Recent WWChar records
        var allWWChars = await _wwCharService.GetAllAsync(companyId);
        viewModel.RecentWWCharRecords = allWWChars.Count(r => r.CreatedDate >= thirtyDaysAgo);

        // Recent GWMonit records
        var recentGWMonits = await _gwMonitService.GetByDateRangeAsync(companyId, thirtyDaysAgo, DateTime.UtcNow);
        viewModel.RecentGWMonitRecords = recentGWMonits.Count();

        // Compliance statistics from reports
        var reports = await _irrRprtService.GetAllAsync(companyId);
        var recentReports = reports.Where(r => r.CreatedDate >= thirtyDaysAgo).ToList();
        
        viewModel.CompliantReports = recentReports.Count(r => r.ComplianceStatus == ComplianceStatusEnum.Compliant);
        viewModel.NonCompliantReports = recentReports.Count(r => r.ComplianceStatus == ComplianceStatusEnum.NonCompliant);
        viewModel.UnderReviewReports = recentReports.Count(r => r.ComplianceStatus == ComplianceStatusEnum.UnderReview);

        // Build recent activities list
        var activities = new List<RecentActivityViewModel>();

        // Add recent operator logs
        foreach (var log in recentLogs.Take(5))
        {
            activities.Add(new RecentActivityViewModel
            {
                Type = "Operator Log",
                Description = $"Log entry for {log.Facility?.Name ?? "Facility"}",
                Date = log.CreatedDate,
                User = log.CreatedBy
            });
        }

        // Add recent irrigations
        foreach (var irrigation in recentIrrigations.Take(5))
        {
            activities.Add(new RecentActivityViewModel
            {
                Type = "Irrigation",
                Description = $"Irrigation recorded for {irrigation.Sprayfield?.FieldId ?? "Sprayfield"}",
                Date = irrigation.CreatedDate,
                User = irrigation.CreatedBy
            });
        }

        // Add recent reports
        foreach (var report in recentReports.Take(5))
        {
            activities.Add(new RecentActivityViewModel
            {
                Type = "Report",
                Description = $"Monthly report generated for {report.Facility?.Name ?? "Facility"} - {report.Month} {report.Year}",
                Date = report.CreatedDate,
                User = report.CreatedBy
            });
        }

        viewModel.RecentActivities = activities
            .OrderByDescending(a => a.Date)
            .Take(10)
            .ToList();

        // Build compliance summaries
        var complianceSummaries = new List<ComplianceSummaryViewModel>();
        foreach (var report in recentReports.OrderByDescending(r => r.CreatedDate).Take(10))
        {
            var facility = await _facilityService.GetByIdAsync(report.FacilityId);
            complianceSummaries.Add(new ComplianceSummaryViewModel
            {
                FacilityName = facility?.Name ?? "Unknown",
                Period = $"{report.Month} {report.Year}",
                ComplianceStatus = report.ComplianceStatus.ToString(),
                ReportDate = report.CreatedDate
            });
        }

        viewModel.ComplianceSummaries = complianceSummaries;

        ViewBag.IsGlobalAdmin = isGlobalAdmin;

        return View(viewModel);
    }
}

