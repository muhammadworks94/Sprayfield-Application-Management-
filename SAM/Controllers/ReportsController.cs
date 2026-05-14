using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System.Security.Claims;
using System.Text.RegularExpressions;
using SAM.Controllers.Base;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Authorization;
using SAM.Services.Interfaces;
using SAM.Services.Models;
using SAM.ViewModels.OperationalData;
using SAM.ViewModels.Reports;

namespace SAM.Controllers;

/// <summary>
/// Controller for Reports module - read-only reports and compliance indicators.
/// </summary>
[Authorize]
public class ReportsController : BaseController
{
    private readonly IIrrRprtService _irrRprtService;
    private readonly IFacilityService _facilityService;
    private readonly INDAR1Service _ndar1Service;
    private readonly INDAR1RowEditService _ndar1RowEditService;
    private readonly ISprayfieldService _sprayfieldService;
    private readonly INDMRService _ndmrService;
    private readonly INDMLRService _ndmlrService;
    private readonly IGWMonitService _gwMonitService;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ReportsController(
        IIrrRprtService irrRprtService,
        IFacilityService facilityService,
        INDAR1Service ndar1Service,
        INDAR1RowEditService ndar1RowEditService,
        ISprayfieldService sprayfieldService,
        INDMRService ndmrService,
        INDMLRService ndmlrService,
        IGWMonitService gwMonitService,
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        UserManager<ApplicationUser> userManager,
        ILogger<ReportsController> logger)
        : base(userManager, logger)
    {
        _irrRprtService = irrRprtService;
        _facilityService = facilityService;
        _ndar1Service = ndar1Service;
        _ndar1RowEditService = ndar1RowEditService;
        _sprayfieldService = sprayfieldService;
        _ndmrService = ndmrService;
        _ndmlrService = ndmlrService;
        _gwMonitService = gwMonitService;
        _context = context;
        _environment = environment;
    }

    #region NDMR Reports

    [HttpGet]
    public async Task<IActionResult> NDMRReports(
        Guid? companyId = null,
        Guid? facilityId = null,
        int? month = null,
        int? year = null,
        string? sortBy = null,
        string? sortDir = null,
        int page = 1,
        int pageSize = 25)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // Use effective company ID if no companyId specified (respects session selection for admins)
        if (!companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        if (companyId.HasValue)
        {
            await EnsureCompanyAccessAsync(companyId.Value);
        }

        var normalizedPageSize = pageSize <= 0 ? 25 : Math.Min(pageSize, 200);
        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedSortBy = string.IsNullOrWhiteSpace(sortBy) ? "period" : sortBy.Trim().ToLowerInvariant();
        var normalizedSortDir = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
        var normalizedMonth = month.HasValue && month.Value >= 1 && month.Value <= 12 ? month : null;
        var normalizedYear = year.HasValue && year.Value >= 2000 && year.Value <= 2100 ? year : null;

        var query = _context.IrrRprts
            .Include(r => r.Company)
            .Include(r => r.Facility)
            .AsNoTracking()
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(r => r.CompanyId == companyId.Value);
        }

        if (facilityId.HasValue)
        {
            query = query.Where(r => r.FacilityId == facilityId.Value);
        }

        if (normalizedMonth.HasValue)
        {
            var monthValue = (MonthEnum)normalizedMonth.Value;
            query = query.Where(r => r.Month == monthValue);
        }

        if (normalizedYear.HasValue)
        {
            query = query.Where(r => r.Year == normalizedYear.Value);
        }

        query = normalizedSortBy switch
        {
            "facility" => normalizedSortDir == "asc"
                ? query.OrderBy(r => r.Facility!.Name).ThenByDescending(r => r.CreatedDate)
                : query.OrderByDescending(r => r.Facility!.Name).ThenByDescending(r => r.CreatedDate),
            "totalvolume" => normalizedSortDir == "asc"
                ? query.OrderBy(r => r.TotalVolumeApplied).ThenByDescending(r => r.CreatedDate)
                : query.OrderByDescending(r => r.TotalVolumeApplied).ThenByDescending(r => r.CreatedDate),
            "compliance" => normalizedSortDir == "asc"
                ? query.OrderBy(r => r.ComplianceStatus).ThenByDescending(r => r.CreatedDate)
                : query.OrderByDescending(r => r.ComplianceStatus).ThenByDescending(r => r.CreatedDate),
            _ => normalizedSortDir == "asc"
                ? query.OrderBy(r => r.Year).ThenBy(r => (int)r.Month).ThenByDescending(r => r.CreatedDate)
                : query.OrderByDescending(r => r.Year).ThenByDescending(r => (int)r.Month).ThenByDescending(r => r.CreatedDate)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync();

        var viewModels = items.Select(r => new IrrRprtViewModel
        {
            Id = r.Id,
            CompanyId = r.CompanyId,
            CompanyName = r.Company?.Name,
            FacilityId = r.FacilityId,
            FacilityName = r.Facility?.Name,
            Month = r.Month,
            Year = r.Year,
            TotalVolumeApplied = r.TotalVolumeApplied,
            TotalApplicationRate = r.TotalApplicationRate,
            HydraulicLoadingRate = r.HydraulicLoadingRate,
            NitrogenLoadingRate = r.NitrogenLoadingRate,
            PanUptakeRate = r.PanUptakeRate,
            ApplicationEfficiency = r.ApplicationEfficiency,
            WeatherSummary = r.WeatherSummary,
            OperationalNotes = r.OperationalNotes,
            ComplianceStatus = r.ComplianceStatus,
            CreatedDate = r.CreatedDate,
            UpdatedDate = r.UpdatedDate
        }).ToList();

        var model = new IrrRprtReportsIndexViewModel
        {
            IsGlobalAdmin = isGlobalAdmin,
            SelectedCompanyId = companyId,
            Facilities = await GetFacilitySelectListAsync(companyId),
            Filter = new IrrRprtFilterViewModel
            {
                FacilityId = facilityId,
                Month = normalizedMonth,
                Year = normalizedYear,
                Page = normalizedPage,
                PageSize = normalizedPageSize
            },
            Sort = new IrrRprtSortViewModel
            {
                SortBy = normalizedSortBy,
                SortDir = normalizedSortDir
            },
            Reports = new PagedResult<IrrRprtViewModel>
            {
                Items = viewModels,
                TotalCount = totalCount,
                Page = normalizedPage,
                PageSize = normalizedPageSize
            }
        };

        return View("IrrigationReports", model);
    }

    [HttpGet]
    public async Task<IActionResult> NDMRReportDetails(Guid id)
    {
        var report = await _irrRprtService.GetByIdAsync(id);
        if (report == null)
            return NotFound();

        await EnsureCompanyAccessAsync(report.CompanyId);

        var viewModel = new IrrRprtViewModel
        {
            Id = report.Id,
            CompanyId = report.CompanyId,
            CompanyName = report.Company?.Name,
            FacilityId = report.FacilityId,
            FacilityName = report.Facility?.Name,
            Month = report.Month,
            Year = report.Year,
            TotalVolumeApplied = report.TotalVolumeApplied,
            TotalApplicationRate = report.TotalApplicationRate,
            HydraulicLoadingRate = report.HydraulicLoadingRate,
            NitrogenLoadingRate = report.NitrogenLoadingRate,
            PanUptakeRate = report.PanUptakeRate,
            ApplicationEfficiency = report.ApplicationEfficiency,
            WeatherSummary = report.WeatherSummary,
            OperationalNotes = report.OperationalNotes,
            ComplianceStatus = report.ComplianceStatus,
            CreatedDate = report.CreatedDate,
            UpdatedDate = report.UpdatedDate
        };

        return View("IrrigationReportDetails", viewModel);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> GenerateNDMRReport(Guid? companyId = null, Guid? facilityId = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // Set company ID if not provided (respects session selection for admins)
        if (!companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        if (companyId.HasValue)
        {
            await EnsureCompanyAccessAsync(companyId.Value);
        }

        var viewModel = new IrrRprtCreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty,
            FacilityId = facilityId ?? Guid.Empty
        };

        ViewBag.Facilities = await GetFacilitySelectListAsync(companyId);
        ViewBag.Months = GetMonthSelectList();

        return View("GenerateIrrigationReport", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> GenerateNDMRReport(IrrRprtCreateViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Months = GetMonthSelectList();
            return View("GenerateIrrigationReport", viewModel);
        }

        try
        {
            // Generate the report
            var report = await _irrRprtService.GenerateMonthlyReportAsync(
                viewModel.FacilityId,
                (int)viewModel.Month,
                viewModel.Year);

            // Save the generated report
            var savedReport = await _irrRprtService.CreateAsync(report);

            TempData["SuccessMessage"] = $"Monthly NDMR report generated successfully for {viewModel.Month} {viewModel.Year}.";
            return RedirectToAction(nameof(NDMRReportDetails), new { id = savedReport.Id });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Months = GetMonthSelectList();
            return View("GenerateIrrigationReport", viewModel);
        }
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> NDMRReportEdit(Guid id)
    {
        var report = await _irrRprtService.GetByIdAsync(id);
        if (report == null)
            return NotFound();

        await EnsureCompanyAccessAsync(report.CompanyId);

        var viewModel = new IrrRprtEditViewModel
        {
            Id = report.Id,
            CompanyId = report.CompanyId,
            FacilityId = report.FacilityId,
            Month = report.Month,
            Year = report.Year,
            TotalVolumeApplied = report.TotalVolumeApplied,
            TotalApplicationRate = report.TotalApplicationRate,
            HydraulicLoadingRate = report.HydraulicLoadingRate,
            NitrogenLoadingRate = report.NitrogenLoadingRate,
            PanUptakeRate = report.PanUptakeRate,
            ApplicationEfficiency = report.ApplicationEfficiency,
            WeatherSummary = report.WeatherSummary,
            OperationalNotes = report.OperationalNotes,
            ComplianceStatus = report.ComplianceStatus
        };

        ViewBag.Facilities = await GetFacilitySelectListAsync(report.CompanyId);
        ViewBag.Months = GetMonthSelectList();
        ViewBag.ComplianceStatuses = GetComplianceStatusSelectList();

        return View("IrrigationReportEdit", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> NDMRReportEdit(IrrRprtEditViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Months = GetMonthSelectList();
            ViewBag.ComplianceStatuses = GetComplianceStatusSelectList();
            return View("IrrigationReportEdit", viewModel);
        }

        try
        {
            var report = await _irrRprtService.GetByIdAsync(viewModel.Id);
            if (report == null)
                return NotFound();

            report.Month = viewModel.Month;
            report.Year = viewModel.Year;
            report.TotalVolumeApplied = viewModel.TotalVolumeApplied;
            report.TotalApplicationRate = viewModel.TotalApplicationRate;
            report.HydraulicLoadingRate = viewModel.HydraulicLoadingRate;
            report.NitrogenLoadingRate = viewModel.NitrogenLoadingRate;
            report.PanUptakeRate = viewModel.PanUptakeRate;
            report.ApplicationEfficiency = viewModel.ApplicationEfficiency;
            report.WeatherSummary = viewModel.WeatherSummary;
            report.OperationalNotes = viewModel.OperationalNotes;
            report.ComplianceStatus = viewModel.ComplianceStatus;

            await _irrRprtService.UpdateAsync(report);
            TempData["SuccessMessage"] = "Monthly NDMR report updated successfully.";
            return RedirectToAction(nameof(NDMRReportDetails), new { id = report.Id });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Months = GetMonthSelectList();
            ViewBag.ComplianceStatuses = GetComplianceStatusSelectList();
            return View("IrrigationReportEdit", viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> NDMRReportDelete(Guid id)
    {
        IrrRprt? report = null;
        try
        {
            report = await _irrRprtService.GetByIdAsync(id);
            if (report == null)
                return NotFound();

            await EnsureCompanyAccessAsync(report.CompanyId);

            await _irrRprtService.DeleteAsync(id);
            TempData["SuccessMessage"] = "NDMR report deleted successfully.";
            return RedirectToAction(nameof(NDMRReports), new {  facilityId = report.FacilityId });
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "NDMR report not found.";
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(NDMRReports), new { facilityId = report?.FacilityId });
    }

    #endregion

    #region Legacy Irrigation Routes

    [HttpGet]
    public IActionResult IrrigationReports(Guid? companyId = null, Guid? facilityId = null)
        => RedirectToActionPermanent(nameof(NDMRReports), new { companyId, facilityId });

    [HttpGet]
    public IActionResult IrrigationReportDetails(Guid id)
        => RedirectToActionPermanent(nameof(NDMRReportDetails), new { id });

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public IActionResult GenerateIrrigationReport(Guid? companyId = null, Guid? facilityId = null)
        => RedirectToActionPermanent(nameof(GenerateNDMRReport), new { companyId, facilityId });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> GenerateIrrigationReport(IrrRprtCreateViewModel viewModel)
        => await GenerateNDMRReport(viewModel);

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public IActionResult IrrigationReportEdit(Guid id)
        => RedirectToActionPermanent(nameof(NDMRReportEdit), new { id });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> IrrigationReportEdit(IrrRprtEditViewModel viewModel)
        => await NDMRReportEdit(viewModel);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> IrrigationReportDelete(Guid id)
        => await NDMRReportDelete(id);

    #endregion

    #region NDAR-1 Reports

    [HttpGet]
    public async Task<IActionResult> NDAR1Reports(
        Guid? facilityId = null,
        string? operatorName = null,
        DateTime? logDate = null,
        string? sortBy = null,
        string? sortDir = null,
        int page = 1,
        int pageSize = 25)
    {
        Guid? companyId = null;
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // Use effective company ID if no companyId specified (respects session selection for admins)
        if (!companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        if (companyId.HasValue)
        {
            await EnsureCompanyAccessAsync(companyId.Value);
        }

        var normalizedPageSize = pageSize <= 0 ? 25 : Math.Min(pageSize, 200);
        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedSortBy = string.IsNullOrWhiteSpace(sortBy) ? "period" : sortBy.Trim().ToLowerInvariant();
        var normalizedSortDir = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
        var normalizedOperatorName = string.IsNullOrWhiteSpace(operatorName) ? null : operatorName.Trim();

        var query = _context.NDAR1s
            .Include(r => r.Company)
            .Include(r => r.Facility)
            .AsNoTracking()
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(r => r.CompanyId == companyId.Value);
        }

        if (facilityId.HasValue)
        {
            query = query.Where(r => r.FacilityId == facilityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalizedOperatorName))
        {
            var normalizedOperatorLower = normalizedOperatorName.ToLowerInvariant();
            query = query.Where(r =>
                !string.IsNullOrWhiteSpace(r.CreatedBy) &&
                r.CreatedBy.Trim().ToLower() == normalizedOperatorLower);
        }

        if (logDate.HasValue)
        {
            var date = logDate.Value.Date;
            var nextDate = date.AddDays(1);
            query = query.Where(r => r.CreatedDate >= date && r.CreatedDate < nextDate);
        }

        query = normalizedSortBy switch
        {
            "facility" => normalizedSortDir == "asc"
                ? query.OrderBy(r => r.Facility != null ? r.Facility.Name : string.Empty).ThenByDescending(r => r.CreatedDate)
                : query.OrderByDescending(r => r.Facility != null ? r.Facility.Name : string.Empty).ThenByDescending(r => r.CreatedDate),
            "operator" => normalizedSortDir == "asc"
                ? query.OrderBy(r => r.CreatedBy).ThenByDescending(r => r.CreatedDate)
                : query.OrderByDescending(r => r.CreatedBy).ThenByDescending(r => r.CreatedDate),
            "logdate" => normalizedSortDir == "asc"
                ? query.OrderBy(r => r.CreatedDate).ThenByDescending(r => r.CreatedDate)
                : query.OrderByDescending(r => r.CreatedDate).ThenByDescending(r => r.CreatedDate),
            _ => normalizedSortDir == "asc"
                ? query.OrderBy(r => r.Year).ThenBy(r => (int)r.Month).ThenByDescending(r => r.CreatedDate)
                : query.OrderByDescending(r => r.Year).ThenByDescending(r => (int)r.Month).ThenByDescending(r => r.CreatedDate)
        };

        var totalCount = await query.CountAsync();
        var reports = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync();

        var items = reports.Select(r => new NDAR1ViewModel
        {
            Id = r.Id,
            CompanyId = r.CompanyId,
            CompanyName = r.Company?.Name,
            FacilityId = r.FacilityId,
            FacilityName = r.Facility?.Name,
            Month = r.Month,
            Year = r.Year,
            DidIrrigationOccur = r.DidIrrigationOccur,
            CreatedDate = r.CreatedDate,
            UpdatedDate = r.UpdatedDate,
            CreatedBy = r.CreatedBy
        }).ToList();

        var operators = await _context.NDAR1s
            .AsNoTracking()
            .Where(r =>
                (!companyId.HasValue || r.CompanyId == companyId.Value) &&
                (!facilityId.HasValue || r.FacilityId == facilityId.Value) &&
                !string.IsNullOrWhiteSpace(r.CreatedBy))
            .Select(r => r.CreatedBy.Trim())
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var operatorItems = operators.Select(o => new SelectListItem { Value = o, Text = o }).ToList();

        var model = new NDAR1ReportsIndexViewModel
        {
            IsGlobalAdmin = isGlobalAdmin,
            SelectedCompanyId = companyId,
            Facilities = await GetFacilitySelectListAsync(companyId),
            Operators = new SelectList(operatorItems, "Value", "Text", normalizedOperatorName),
            Filter = new NDAR1FilterViewModel
            {
                FacilityId = facilityId,
                OperatorName = normalizedOperatorName,
                LogDate = logDate,
                Page = normalizedPage,
                PageSize = normalizedPageSize
            },
            Sort = new NDAR1SortViewModel
            {
                SortBy = normalizedSortBy,
                SortDir = normalizedSortDir
            },
            Reports = new PagedResult<NDAR1ViewModel>
            {
                Items = items,
                TotalCount = totalCount,
                Page = normalizedPage,
                PageSize = normalizedPageSize
            }
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> NDAR1ReportDetails(Guid id)
    {
        var report = await _ndar1Service.GetByIdAsync(id);
        if (report == null)
            return NotFound();

        await EnsureCompanyAccessAsync(report.CompanyId);

        var viewModel = new NDAR1ViewModel
        {
            Id = report.Id,
            CompanyId = report.CompanyId,
            CompanyName = report.Company?.Name,
            FacilityId = report.FacilityId,
            FacilityName = report.Facility?.Name,
            Month = report.Month,
            Year = report.Year,
            DidIrrigationOccur = report.DidIrrigationOccur,
            CreatedDate = report.CreatedDate,
            UpdatedDate = report.UpdatedDate
        };

        ViewBag.Report = report;
        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> GenerateNDAR1Report(Guid? companyId = null, Guid? facilityId = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // Set company ID if not provided (respects session selection for admins)
        if (!companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        if (companyId.HasValue)
        {
            await EnsureCompanyAccessAsync(companyId.Value);
        }

        var viewModel = new NDAR1CreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty,
            FacilityId = facilityId ?? Guid.Empty
        };

        ViewBag.Facilities = await GetFacilitySelectListAsync(companyId);
        ViewBag.Months = GetMonthSelectList();

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> GenerateNDAR1Report(NDAR1CreateViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Months = GetMonthSelectList();
            return View(viewModel);
        }

        try
        {
            // Reuse an existing monthly report instead of recomputing/overwriting curated data.
            var existing = await _ndar1Service.GetByFacilityMonthYearAsync(
                viewModel.FacilityId,
                (int)viewModel.Month,
                viewModel.Year);

            if (existing != null)
            {
                TempData["SuccessMessage"] = $"NDAR-1 report for {viewModel.Month} {viewModel.Year} already exists. Reusing the existing report. It is automatically synced when related monthly applications or operator logs are updated.";
                return RedirectToAction(nameof(NDAR1ReportDetails), new { id = existing.Id });
            }

            // Generate the report
            var report = await _ndar1Service.GenerateMonthlyReportAsync(
                viewModel.FacilityId,
                (int)viewModel.Month,
                viewModel.Year);

            // Save the generated report
            var savedReport = await _ndar1Service.CreateAsync(report);

            TempData["SuccessMessage"] = $"NDAR-1 report generated successfully for {viewModel.Month} {viewModel.Year}.";
            return RedirectToAction(nameof(NDAR1ReportDetails), new { id = savedReport.Id });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Months = GetMonthSelectList();
            return View(viewModel);
        }
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> NDAR1ReportEdit(Guid id)
    {
        var report = await _ndar1Service.GetByIdAsync(id);
        if (report == null)
            return NotFound();

        await EnsureCompanyAccessAsync(report.CompanyId);
        var grid = await _ndar1RowEditService.BuildGridAsync(id);
        return View(grid);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> NDAR1BeginRowEdit(Guid ndar1Id, int dayNo)
    {
        var report = await _ndar1Service.GetByIdAsync(ndar1Id);
        if (report == null)
        {
            return NotFound();
        }

        await EnsureCompanyAccessAsync(report.CompanyId);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var userDisplay = User.FindFirstValue("FullName") ?? User.Identity?.Name ?? "User";
        var result = await _ndar1RowEditService.BeginRowEditAsync(ndar1Id, dayNo, userId, userDisplay);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> NDAR1UpdateRow([FromBody] NDAR1DayRowUpdateRequest request, Guid ndar1Id)
    {
        try
        {
            var report = await _ndar1Service.GetByIdAsync(ndar1Id);
            if (report == null)
            {
                return NotFound();
            }

            await EnsureCompanyAccessAsync(report.CompanyId);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _ndar1RowEditService.UpdateRowAsync(ndar1Id, request, userId);
            if (!result.Success)
            {
                Response.StatusCode = result.IsValidationError ? 400 : 409;
            }
            return Json(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "NDAR1 row update failed for NDAR1 {Ndar1Id}, day {DayNo}", ndar1Id, request?.DayNo);
            return StatusCode(500, new
            {
                success = false,
                message = "We couldn't update this row right now. Please try again."
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> NDAR1CancelRowEdit(Guid ndar1Id, int dayNo, Guid lockToken)
    {
        var report = await _ndar1Service.GetByIdAsync(ndar1Id);
        if (report == null)
        {
            return NotFound();
        }

        await EnsureCompanyAccessAsync(report.CompanyId);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _ndar1RowEditService.CancelRowEditAsync(ndar1Id, dayNo, lockToken, userId);
        return Ok(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> NDAR1ReportEdit(NDAR1EditViewModel viewModel)
    {
        if (viewModel == null)
        {
            return BadRequest("Unable to read submitted form data. Please refresh the page and try again.");
        }

        // Phase 1: legacy full-form NDAR edits are disabled in favor of row-based live-source editing.
        TempData["ErrorMessage"] = "This edit path is no longer available. Please use the row-based Edit/Update actions in the daily grid.";
        return RedirectToAction(nameof(NDAR1ReportEdit), new { id = viewModel.Id });
    }

    private static List<NDAR1FieldEditViewModel> BuildNdarFieldEditModels(NDAR1 report)
    {
        if (report.Fields.Any())
        {
            return report.Fields
                .OrderBy(f => f.FieldOrder)
                .Select(f => new NDAR1FieldEditViewModel
                {
                    Id = f.Id,
                    SprayfieldId = f.SprayfieldId,
                    FieldCode = f.Sprayfield?.FieldId ?? string.Empty,
                    Acres = f.Sprayfield?.SizeAcres,
                    CropSummary = f.Sprayfield != null ? SAM.Utilities.SprayfieldZoneSummaryHelper.GetCropSummary(f.Sprayfield) ?? string.Empty : string.Empty,
                    MonthlyLoading = f.MonthlyLoading,
                    MaxHourlyLoading = f.MaxHourlyLoading,
                    TwelveMonthFloatingTotal = f.TwelveMonthFloatingTotal,
                    DailyValues = f.DailyValues
                        .OrderBy(d => d.DayNo)
                        .Select(d => new NDAR1FieldDailyEditViewModel
                        {
                            DayNo = d.DayNo,
                            VolumeApplied = d.VolumeApplied,
                            TimeIrrigated = d.TimeIrrigated,
                            DailyLoading = d.DailyLoading,
                            MaxHourlyLoading = d.MaxHourlyLoading
                        })
                        .ToList()
                })
                .ToList();
        }

        var legacy = new List<NDAR1FieldEditViewModel>();
        AddLegacyFieldModel(legacy, report.Field1Id, report.Field1, 1, report.Field1MonthlyLoading, report.Field1MaxHourlyLoading, report.Field1TwelveMonthFloatingTotal, report.Field1VolumeAppliedDaily, report.Field1TimeIrrigatedDaily, report.Field1DailyLoadingDaily, report.Field1MaxHourlyLoadingDaily);
        AddLegacyFieldModel(legacy, report.Field2Id, report.Field2, 2, report.Field2MonthlyLoading, report.Field2MaxHourlyLoading, report.Field2TwelveMonthFloatingTotal, report.Field2VolumeAppliedDaily, report.Field2TimeIrrigatedDaily, report.Field2DailyLoadingDaily, report.Field2MaxHourlyLoadingDaily);
        AddLegacyFieldModel(legacy, report.Field3Id, report.Field3, 3, report.Field3MonthlyLoading, report.Field3MaxHourlyLoading, report.Field3TwelveMonthFloatingTotal, report.Field3VolumeAppliedDaily, report.Field3TimeIrrigatedDaily, report.Field3DailyLoadingDaily, report.Field3MaxHourlyLoadingDaily);
        AddLegacyFieldModel(legacy, report.Field4Id, report.Field4, 4, report.Field4MonthlyLoading, report.Field4MaxHourlyLoading, report.Field4TwelveMonthFloatingTotal, report.Field4VolumeAppliedDaily, report.Field4TimeIrrigatedDaily, report.Field4DailyLoadingDaily, report.Field4MaxHourlyLoadingDaily);
        return legacy;
    }

    private static void AddLegacyFieldModel(
        List<NDAR1FieldEditViewModel> target,
        Guid? sprayfieldId,
        Sprayfield? sprayfield,
        int order,
        decimal monthlyLoading,
        decimal maxHourlyLoading,
        decimal floatingTotal,
        List<decimal?> volumeDaily,
        List<decimal?> timeDaily,
        List<decimal?> loadingDaily,
        List<decimal?> maxHourlyDaily)
    {
        if (!sprayfieldId.HasValue)
        {
            return;
        }

        var model = new NDAR1FieldEditViewModel
        {
            SprayfieldId = sprayfieldId.Value,
            FieldCode = sprayfield?.FieldId ?? order.ToString(),
            Acres = sprayfield?.SizeAcres,
            CropSummary = sprayfield != null ? SAM.Utilities.SprayfieldZoneSummaryHelper.GetCropSummary(sprayfield) ?? string.Empty : string.Empty,
            MonthlyLoading = monthlyLoading,
            MaxHourlyLoading = maxHourlyLoading,
            TwelveMonthFloatingTotal = floatingTotal
        };

        for (var day = 1; day <= 31; day++)
        {
            var idx = day - 1;
            model.DailyValues.Add(new NDAR1FieldDailyEditViewModel
            {
                DayNo = day,
                VolumeApplied = idx < volumeDaily.Count ? volumeDaily[idx] : null,
                TimeIrrigated = idx < timeDaily.Count ? timeDaily[idx] : null,
                DailyLoading = idx < loadingDaily.Count ? loadingDaily[idx] : null,
                MaxHourlyLoading = idx < maxHourlyDaily.Count ? maxHourlyDaily[idx] : null
            });
        }

        target.Add(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> NDAR1ReportDelete(Guid id)
    {
        NDAR1? report = null;

        try
        {
            report = await _ndar1Service.GetByIdAsync(id);
            if (report == null)
                return NotFound();

            await EnsureCompanyAccessAsync(report.CompanyId);

            await _ndar1Service.DeleteAsync(id);
            TempData["SuccessMessage"] = "NDAR-1 report deleted successfully.";
            return RedirectToAction(nameof(NDAR1Reports), new { facilityId = report.FacilityId });
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "NDAR-1 report not found.";
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(NDAR1Reports), new { facilityId = report?.FacilityId });
    }

    [HttpGet]
    public async Task<IActionResult> ExportNDAR1Report(Guid id)
    {
        var report = await _ndar1Service.GetByIdAsync(id);
        if (report == null)
            return NotFound();

        await EnsureCompanyAccessAsync(report.CompanyId);

        try
        {
            var excelBytes = await _ndar1Service.ExportToExcelAsync(id);
            var fileName = $"NDAR-1_{report.Facility?.Name}_{report.Month}_{report.Year}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            if (IsFetchRequest())
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "NDAR-1 export failed",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            TempData["ErrorMessage"] = $"Error exporting report: {ex.Message}";
            return RedirectToAction(nameof(NDAR1ReportDetails), new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportNDAR1ReportPdf(Guid id, bool showGrid = false)
    {
        var report = await _ndar1Service.GetByIdAsync(id);
        if (report == null)
            return NotFound();

        await EnsureCompanyAccessAsync(report.CompanyId);

        try
        {
            var pdfBytes = await RenderNdar1PdfAsync(report, showGrid);
            var safeFacility = Regex.Replace(report.Facility?.Name ?? "Facility", @"[^\w\-]+", "_");
            var fileName = $"NDAR-1_{safeFacility}_{report.Month}_{report.Year}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            if (IsFetchRequest())
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "NDAR-1 PDF export failed",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            TempData["ErrorMessage"] = $"Error exporting NDAR-1 PDF: {ex.Message}";
            return RedirectToAction(nameof(NDAR1ReportDetails), new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportNDMRReport(Guid id)
    {
        var report = await _ndar1Service.GetByIdAsync(id);
        if (report == null)
            return NotFound();

        await EnsureCompanyAccessAsync(report.CompanyId);

        try
        {
            var excelBytes = await _ndmrService.ExportToExcelAsync(id);
            var fileName = $"NDMR_{report.Facility?.Name}_{report.Month}_{report.Year}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            if (IsFetchRequest())
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "NDMR export failed",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            TempData["ErrorMessage"] = $"Error exporting NDMR report: {ex.Message}";
            return RedirectToAction(nameof(NDAR1ReportDetails), new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportNDMLRReport(Guid id)
    {
        var report = await _ndar1Service.GetByIdAsync(id);
        if (report == null)
            return NotFound();

        await EnsureCompanyAccessAsync(report.CompanyId);

        try
        {
            var excelBytes = await _ndmlrService.ExportToExcelAsync(id);
            var fileName = $"NDMLR_{report.Facility?.Name}_{report.Month}_{report.Year}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            if (IsFetchRequest())
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "NDMLR export failed",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            TempData["ErrorMessage"] = $"Error exporting NDMLR report: {ex.Message}";
            return RedirectToAction(nameof(NDAR1ReportDetails), new { id });
        }
    }

    #endregion

    #region Groundwater Quality Reports

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> GroundwaterQualityReports(
        Guid? companyId = null,
        Guid? facilityId = null,
        Guid? monitoringWellId = null,
        int? month = null,
        int? year = null,
        string? sortBy = null,
        string? sortDir = null,
        int page = 1,
        int pageSize = 25)
    {
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();
        if (!companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        if (companyId.HasValue)
        {
            await EnsureCompanyAccessAsync(companyId.Value);
        }

        var normalizedPageSize = pageSize <= 0 ? 25 : Math.Min(pageSize, 200);
        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedSortBy = string.IsNullOrWhiteSpace(sortBy) ? "sampledate" : sortBy.Trim().ToLowerInvariant();
        var normalizedSortDir = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
        var normalizedMonth = month.HasValue && month.Value >= 1 && month.Value <= 12 ? month : null;
        var normalizedYear = year.HasValue && year.Value >= 2000 && year.Value <= 2100 ? year : null;

        var query = _context.GWMonits
            .Include(x => x.Facility)
            .Include(x => x.MonitoringWell)
            .AsNoTracking()
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(x => x.CompanyId == companyId.Value);
        }

        if (facilityId.HasValue)
        {
            query = query.Where(x => x.FacilityId == facilityId.Value);
        }

        if (monitoringWellId.HasValue)
        {
            query = query.Where(x => x.MonitoringWellId == monitoringWellId.Value);
        }

        if (normalizedMonth.HasValue)
        {
            query = query.Where(x => x.SampleDate.Month == normalizedMonth.Value);
        }

        if (normalizedYear.HasValue)
        {
            query = query.Where(x => x.SampleDate.Year == normalizedYear.Value);
        }

        query = normalizedSortBy switch
        {
            "facility" => normalizedSortDir == "asc"
                ? query.OrderBy(x => x.Facility!.Name).ThenByDescending(x => x.CreatedDate)
                : query.OrderByDescending(x => x.Facility!.Name).ThenByDescending(x => x.CreatedDate),
            "well" => normalizedSortDir == "asc"
                ? query.OrderBy(x => x.MonitoringWell!.WellId).ThenByDescending(x => x.CreatedDate)
                : query.OrderByDescending(x => x.MonitoringWell!.WellId).ThenByDescending(x => x.CreatedDate),
            "collectedby" => normalizedSortDir == "asc"
                ? query.OrderBy(x => x.CollectedBy).ThenByDescending(x => x.CreatedDate)
                : query.OrderByDescending(x => x.CollectedBy).ThenByDescending(x => x.CreatedDate),
            "analyzedby" => normalizedSortDir == "asc"
                ? query.OrderBy(x => x.AnalyzedBy).ThenByDescending(x => x.CreatedDate)
                : query.OrderByDescending(x => x.AnalyzedBy).ThenByDescending(x => x.CreatedDate),
            _ => normalizedSortDir == "asc"
                ? query.OrderBy(x => x.SampleDate).ThenByDescending(x => x.CreatedDate)
                : query.OrderByDescending(x => x.SampleDate).ThenByDescending(x => x.CreatedDate)
        };

        var totalCount = await query.CountAsync();
        var pagedRecords = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync();

        var rows = new List<GroundwaterQualityReportRowViewModel>();
        foreach (var record in pagedRecords)
        {
            var permit = await ResolvePermitForDateAsync(record.FacilityId, record.SampleDate);
            var permitDisplay = permit == null
                ? "No active permit"
                : $"{permit.PermitNumber} v{permit.PermitVersion}";

            rows.Add(new GroundwaterQualityReportRowViewModel
            {
                Id = record.Id,
                FacilityId = record.FacilityId,
                FacilityName = record.Facility?.Name ?? string.Empty,
                MonitoringWellName = record.MonitoringWell?.WellId ?? string.Empty,
                SampleDate = record.SampleDate,
                PermitDisplay = permitDisplay,
                CollectedBy = record.CollectedBy,
                AnalyzedBy = record.AnalyzedBy,
                UpdatedDate = record.UpdatedDate
            });
        }

        var model = new GroundwaterQualityReportsPageViewModel
        {
            SelectedCompanyId = companyId,
            Filter = new GroundwaterQualityFilterViewModel
            {
                FacilityId = facilityId,
                MonitoringWellId = monitoringWellId,
                Month = normalizedMonth,
                Year = normalizedYear,
                Page = normalizedPage,
                PageSize = normalizedPageSize
            },
            Sort = new GroundwaterQualitySortViewModel
            {
                SortBy = normalizedSortBy,
                SortDir = normalizedSortDir
            },
            Reports = new PagedResult<GroundwaterQualityReportRowViewModel>
            {
                Items = rows,
                TotalCount = totalCount,
                Page = normalizedPage,
                PageSize = normalizedPageSize
            }
        };

        var facilities = await _facilityService.GetAllAsync(companyId);
        model.Facilities = new SelectList(facilities, "Id", "Name", facilityId);
        var wells = companyId.HasValue
            ? await _context.MonitoringWells.AsNoTracking()
                .Where(x => x.CompanyId == companyId.Value)
                .OrderBy(x => x.WellId)
                .ToListAsync()
            : new List<MonitoringWell>();
        if (facilityId.HasValue)
        {
            var wellIdsForFacility = await _context.GWMonits.AsNoTracking()
                .Where(x => x.FacilityId == facilityId.Value)
                .Select(x => x.MonitoringWellId)
                .Distinct()
                .ToListAsync();
            wells = wells.Where(x => wellIdsForFacility.Contains(x.Id)).ToList();
        }
        model.MonitoringWells = new SelectList(wells, "Id", "WellId", monitoringWellId);
        model.Months = new SelectList(
            Enum.GetValues(typeof(MonthEnum)).Cast<MonthEnum>()
                .Select(e => new SelectListItem { Value = ((int)e).ToString(), Text = e.ToString() }),
            "Value",
            "Text",
            normalizedMonth?.ToString());
        model.Years = Enumerable.Range(DateTime.UtcNow.Year - 10, 21)
            .Select(y => new SelectListItem(y.ToString(), y.ToString()))
            .ToList();

        return View(model);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> PreviewGW59Report(Guid id)
    {
        var model = await BuildGw59ExportModelAsync(id);
        await EnsureCompanyAccessAsync((await _gwMonitService.GetByIdAsync(id))!.CompanyId);
        var preview = new GW59ReportViewModel
        {
            GwMonitId = model.GwMonitId,
            FacilityName = model.FacilityName,
            PermitNumber = model.PermitNumber,
            Permittee = model.Permittee,
            Address = model.Address,
            City = model.City,
            State = model.State,
            ZipCode = model.ZipCode,
            County = model.County,
            FacilityPhone = model.FacilityPhone,
            PermitExpirationDate = model.PermitExpirationDate,
            WellId = model.WellId,
            WellLocation = model.WellLocation,
            WellDepthFeet = model.WellDepthFeet,
            DiameterInches = model.DiameterInches,
            LowScreenDepthFeet = model.LowScreenDepthFeet,
            HighScreenDepthFeet = model.HighScreenDepthFeet,
            NumberOfWellsToBeSampled = model.NumberOfWellsToBeSampled,
            SampleDate = model.SampleDate,
            SampleDepth = model.SampleDepth,
            WaterLevel = model.WaterLevel,
            GallonsPumped = model.GallonsPumped,
            PHField = model.PHField,
            TemperatureField = model.TemperatureField,
            SpecificConductance = model.SpecificConductance,
            Odor = model.Odor,
            Appearance = model.Appearance,
            MetalsUnfiltered = model.MetalsUnfiltered,
            MetalsAcidified = model.MetalsAcidified,
            TDS = model.TDS,
            TOC = model.TOC,
            Chloride = model.Chloride,
            NH3N = model.NH3N,
            NO3N = model.NO3N,
            TKN = model.TKN,
            Calcium = model.Calcium,
            Magnesium = model.Magnesium,
            FecalColiform = model.FecalColiform,
            TotalColiform = model.TotalColiform,
            LabName = model.LabName,
            LabCertificationNumber = model.LabCertificationNumber,
            LabReportAttached = model.LabReportAttached,
            VOCMethodNumber = model.VOCMethodNumber,
            CertificationName = model.CertificationName,
            CertificationTitle = model.CertificationTitle,
            CertificationDate = model.CertificationDate
        };
        return View("~/Views/OperationalData/GWMonitReport.cshtml", preview);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> ExportGW59Report(Guid id)
    {
        var model = await BuildGw59ExportModelAsync(id);
        var bytes = await RenderGw59PdfAsync(model);
        var safeFacility = string.IsNullOrWhiteSpace(model.FacilityName) ? "Facility" : model.FacilityName.Replace(' ', '_');
        return File(bytes, "application/pdf", $"GW59_{safeFacility}_{model.SampleDate:yyyyMMdd}.pdf");
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> ExportGW59AReport(Guid id, bool showGrid = false)
    {
        var model = await BuildGw59ExportModelAsync(id);
        if (IsGw59AQuestionnaireEmpty(model))
        {
            throw new Infrastructure.Exceptions.BusinessRuleException(
                "GW-59A has no compliance questionnaire data for this record. Fill the 'GW-59A Compliance' section on the Groundwater Monitoring create/edit page, save, then export again.");
        }
        var bytes = await RenderGw59APdfAsync(model, showGrid);
        var safeFacility = string.IsNullOrWhiteSpace(model.FacilityName) ? "Facility" : model.FacilityName.Replace(' ', '_');
        return File(bytes, "application/pdf", $"GW59A_{safeFacility}_{model.SampleDate:yyyyMMdd}_{DateTime.UtcNow:HHmmss}.pdf");
    }

    #endregion

    #region Helper Methods

    private async Task<SelectList> GetFacilitySelectListAsync(Guid? companyId = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // Use effective company ID if no companyId specified (respects session selection for admins)
        if (!companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        var facilities = await _facilityService.GetAllAsync(companyId);
        return new SelectList(facilities, "Id", "Name");
    }

    private SelectList GetMonthSelectList()
    {
        return new SelectList(Enum.GetValues(typeof(SAM.Domain.Enums.MonthEnum)).Cast<SAM.Domain.Enums.MonthEnum>()
            .Select(e => new SelectListItem
            {
                Value = ((int)e).ToString(),
                Text = e.ToString()
            }), "Value", "Text");
    }

    private SelectList GetComplianceStatusSelectList()
    {
        return new SelectList(Enum.GetValues(typeof(SAM.Domain.Enums.ComplianceStatusEnum)).Cast<SAM.Domain.Enums.ComplianceStatusEnum>()
            .Select(e => new SelectListItem
            {
                Value = e.ToString(),
                Text = e.ToString()
            }), "Value", "Text");
    }

    private async Task<SelectList> GetSprayfieldSelectListAsync(Guid facilityId)
    {
        var sprayfields = (await _sprayfieldService.GetByFacilityIdAsync(facilityId))
            .OrderBy(s => BuildNaturalSortKey(s.FieldId))
            .ThenBy(s => s.FieldId)
            .ToList();
        return new SelectList(sprayfields, "Id", "FieldId");
    }

    private static string BuildNaturalSortKey(string? input)
    {
        return Regex.Replace(input ?? string.Empty, @"\d+", match => match.Value.PadLeft(10, '0'));
    }

    private bool IsFetchRequest()
    {
        if (Request.Headers.TryGetValue("X-Requested-With", out var requestedWith) &&
            string.Equals(requestedWith.ToString(), "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Request.Headers.TryGetValue("Accept", out var acceptHeader) &&
            acceptHeader.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private async Task<FacilityPermit?> ResolvePermitForDateAsync(Guid facilityId, DateTime date)
    {
        var targetDate = date.Date;
        return await _context.FacilityPermits
            .AsNoTracking()
            .Where(x =>
                x.FacilityId == facilityId &&
                x.IsActive &&
                !x.IsDeleted &&
                x.EffectiveStartDate <= targetDate &&
                (!x.EffectiveEndDate.HasValue || x.EffectiveEndDate.Value >= targetDate))
            .OrderByDescending(x => x.EffectiveStartDate)
            .FirstOrDefaultAsync();
    }

    private async Task<Gw59ExportModel> BuildGw59ExportModelAsync(Guid gwMonitId)
    {
        var gwMonit = await _context.GWMonits
            .AsNoTracking()
            .Include(x => x.Facility)
            .Include(x => x.MonitoringWell)
            .FirstOrDefaultAsync(x => x.Id == gwMonitId);
        if (gwMonit == null)
        {
            throw new Infrastructure.Exceptions.EntityNotFoundException(nameof(GWMonit), gwMonitId);
        }

        await EnsureCompanyAccessAsync(gwMonit.CompanyId);
        var permit = await ResolvePermitForDateAsync(gwMonit.FacilityId, gwMonit.SampleDate);
        var templateValues = await _context.GWMonitTemplateValues
            .AsNoTracking()
            .Where(x => x.GWMonitId == gwMonitId)
            .Include(x => x.FacilityPermitTemplateParameter)
                .ThenInclude(p => p!.PcsParameterCatalog)
            .ToListAsync();

        var snapshots = templateValues
            .Where(x => x.FacilityPermitTemplateParameter != null)
            .Select(x => new Gw59ParameterSnapshot
            {
                PcsCode = x.FacilityPermitTemplateParameter!.PcsParameterCatalog?.PcsCode ?? string.Empty,
                ParameterName = x.FacilityPermitTemplateParameter.ParameterDisplayOverride
                    ?? x.FacilityPermitTemplateParameter.PcsParameterCatalog?.UserFriendlyName
                    ?? x.FacilityPermitTemplateParameter.PcsParameterCatalog?.OfficialParameterName
                    ?? string.Empty,
                Units = x.FacilityPermitTemplateParameter.UnitsOverride
                    ?? x.FacilityPermitTemplateParameter.PcsParameterCatalog?.AcceptedUnits
                    ?? string.Empty,
                Value = x.NumericValue,
                DailyMaximumLimit = x.FacilityPermitTemplateParameter.DailyMaximumLimit,
                IsGw59A = (x.FacilityPermitTemplateParameter.ReportTypes & PermitTemplateReportTypeEnum.Gw59A) != 0
            })
            .OrderBy(x => x.PcsCode)
            .ToList();

        var hasGw59APermitTemplateRows = permit != null && await _context.FacilityPermitTemplateParameters
            .AsNoTracking()
            .AnyAsync(x => x.FacilityPermitId == permit.Id && (x.ReportTypes & PermitTemplateReportTypeEnum.Gw59A) != 0);

        var currentUser = await GetCurrentUserAsync();
        return new Gw59ExportModel
        {
            GwMonitId = gwMonit.Id,
            FacilityName = gwMonit.Facility?.Name ?? string.Empty,
            PermitNumber = permit?.PermitNumber ?? gwMonit.Facility?.PermitNumber ?? string.Empty,
            Permittee = gwMonit.Facility?.Permittee ?? string.Empty,
            Address = gwMonit.Facility?.Address ?? string.Empty,
            City = gwMonit.Facility?.City ?? string.Empty,
            State = gwMonit.Facility?.State ?? string.Empty,
            ZipCode = gwMonit.Facility?.ZipCode ?? string.Empty,
            County = gwMonit.Facility?.County ?? string.Empty,
            FacilityPhone = gwMonit.Facility?.FacilityPhone ?? string.Empty,
            PermitExpirationDate = permit?.EffectiveEndDate ?? gwMonit.Facility?.PermitExpirationDate,
            WellId = gwMonit.MonitoringWell?.WellId ?? string.Empty,
            WellLocation = gwMonit.MonitoringWell?.LocationDescription ?? string.Empty,
            WellDepthFeet = gwMonit.MonitoringWell?.WellDepthFeet,
            DiameterInches = gwMonit.MonitoringWell?.DiameterInches,
            LowScreenDepthFeet = gwMonit.MonitoringWell?.LowScreenDepthFeet,
            HighScreenDepthFeet = gwMonit.MonitoringWell?.HighScreenDepthFeet,
            NumberOfWellsToBeSampled = gwMonit.MonitoringWell?.NumberOfWellsToBeSampled,
            SampleDate = gwMonit.SampleDate,
            SampleDepth = gwMonit.SampleDepth,
            WaterLevel = gwMonit.WaterLevel,
            GallonsPumped = gwMonit.GallonsPumped,
            PHField = gwMonit.PH,
            TemperatureField = gwMonit.Temperature,
            SpecificConductance = gwMonit.Conductivity,
            Odor = gwMonit.Odor,
            Appearance = gwMonit.Appearance,
            MetalsUnfiltered = gwMonit.MetalsSamplesCollectedUnfiltered.GetValueOrDefault(),
            MetalsAcidified = gwMonit.MetalSamplesFieldAcidified.GetValueOrDefault(),
            TDS = gwMonit.TDS,
            TOC = gwMonit.TOC,
            Chloride = gwMonit.Chloride,
            NH3N = gwMonit.NH3N,
            NO3N = gwMonit.NO3N,
            TKN = gwMonit.TKN,
            Calcium = gwMonit.Calcium,
            Magnesium = gwMonit.Magnesium,
            FecalColiform = gwMonit.FecalColiform,
            TotalColiform = gwMonit.TotalColiform,
            LabName = string.IsNullOrWhiteSpace(gwMonit.AnalyzedBy)
                ? (gwMonit.Facility?.CertifiedLaboratory1Name ?? string.Empty)
                : gwMonit.AnalyzedBy,
            LabCertificationNumber = string.IsNullOrWhiteSpace(gwMonit.LabCertification)
                ? (gwMonit.Facility?.LabCertificationNumber1 ?? string.Empty)
                : gwMonit.LabCertification,
            CollectedBy = gwMonit.CollectedBy,
            AnalyzedBy = gwMonit.AnalyzedBy,
            LabReportAttached = gwMonit.VOCReportAttached.GetValueOrDefault(),
            VOCMethodNumber = gwMonit.VOCMethodNumber,
            CertificationName = string.IsNullOrWhiteSpace(currentUser?.FullName) ? (currentUser?.UserName ?? string.Empty) : currentUser!.FullName,
            CertificationTitle = "Authorized Agent",
            CertificationDate = DateTime.UtcNow.Date,
            VOCReportFileStoragePath = gwMonit.VOCReportFileStoragePath,
            HasGw59APermitTemplateRows = hasGw59APermitTemplateRows,
            ParameterSnapshots = snapshots,
            GW59AQuestion1Response = gwMonit.GW59AQuestion1Response,
            GW59AQuestion2Response = gwMonit.GW59AQuestion2Response,
            GW59AQuestion3Response = gwMonit.GW59AQuestion3Response,
            GW59AQuestion4Response = gwMonit.GW59AQuestion4Response,
            GW59AQuestion5Response = gwMonit.GW59AQuestion5Response,
            GW59AQuestion6Response = gwMonit.GW59AQuestion6Response,
            GW59AQuestion7Response = gwMonit.GW59AQuestion7Response,
            GW59ADueDate = gwMonit.GW59ADueDate,
            GW59AQuestion2Details = gwMonit.GW59AQuestion2Details,
            GW59AQuestion4Details = gwMonit.GW59AQuestion4Details,
            GW59AQuestion5Details = gwMonit.GW59AQuestion5Details,
            GW59AQuestion7Details = gwMonit.GW59AQuestion7Details,
            GW59ASignerName = gwMonit.GW59ASignerName,
            GW59ASignerTitle = gwMonit.GW59ASignerTitle,
            GW59ASignedDate = gwMonit.GW59ASignedDate
        };
    }

    private async Task<byte[]> RenderGw59PdfAsync(Gw59ExportModel model)
    {
        var templatePath = Path.Combine(_environment.WebRootPath, "forms", "GW-59 GW-QualityMonitoringReportForm.pdf");
        if (!System.IO.File.Exists(templatePath))
        {
            throw new Infrastructure.Exceptions.BusinessRuleException("GW-59 template PDF not found in wwwroot/forms.");
        }

        using var outputStream = new MemoryStream();
        using (var document = PdfReader.Open(templatePath, PdfDocumentOpenMode.Modify))
        {
            var page = document.Pages[0];
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 8, XFontStyle.Regular);
            void DrawText(string? text, double x, double y) =>
                gfx.DrawString(text ?? string.Empty, font, XBrushes.Black, new XRect(x, y, 260, font.Height + 2), XStringFormats.TopLeft);

            DrawText(model.FacilityName, 120, 74);
            DrawText(model.PermitNumber, 630, 60);
            DrawText(model.Permittee, 170, 90);
            DrawText(model.Address, 120, 106);
            DrawText(model.City, 80, 140);
            DrawText(model.ZipCode, 280, 140);
            DrawText(model.State, 230, 140);
            DrawText(model.County, 410, 120);
            DrawText(model.PermitExpirationDate?.ToString("MM/dd/yyyy"), 775, 60);
            DrawText(model.FacilityPhone, 410, 152);
            DrawText(model.WellId, 210, 204);
            DrawText(model.SampleDate.ToString("MM/dd/yyyy"), 460, 204);
            DrawText(model.WellLocation, 150, 168);
            DrawText(model.WellDepthFeet?.ToString("F2"), 158, 222);
            DrawText(model.DiameterInches?.ToString("F2"), 455, 222);
            if (model.LowScreenDepthFeet.HasValue || model.HighScreenDepthFeet.HasValue)
            {
                DrawText($"{model.LowScreenDepthFeet?.ToString("F2") ?? "?"} to {model.HighScreenDepthFeet?.ToString("F2") ?? "?"} ft", 260, 220);
            }
            DrawText(model.WaterLevel?.ToString("F2"), 160, 237);
            DrawText(model.GallonsPumped?.ToString("F2"), 260, 266);
            DrawText(model.PHField?.ToString("F2"), 615, 220);
            DrawText(model.TemperatureField?.ToString("F1"), 750, 220);
            DrawText(model.SpecificConductance?.ToString("F2"), 660, 238);
            DrawText(model.Odor, 650, 252);
            DrawText(model.Appearance, 650, 267);
            DrawText(model.MetalsUnfiltered ? "X" : string.Empty, 240, 283);
            DrawText(!model.MetalsUnfiltered ? "X" : string.Empty, 301, 282);
            DrawText(model.MetalsAcidified ? "X" : string.Empty, 444, 283);
            DrawText(!model.MetalsAcidified ? "X" : string.Empty, 491, 283);
            DrawText(model.LabName, 450, 308);
            DrawText(model.LabCertificationNumber, 740, 308);
            DrawText(model.TDS?.ToString("F2"), 160, 400);
            DrawText(model.TOC?.ToString("F2"), 165, 430);
            DrawText(model.Chloride?.ToString("F2"), 165, 446);
            DrawText(model.NH3N?.ToString("F2"), 165, 538);
            DrawText(model.TKN?.ToString("F2"), 165, 571);
            DrawText(model.NO3N?.ToString("F2"), 440, 352);
            DrawText(model.Calcium?.ToString("F2"), 440, 430);
            DrawText(model.Magnesium?.ToString("F2"), 440, 538);
            DrawText(model.FecalColiform?.ToString("F0"), 165, 352);
            DrawText(model.TotalColiform?.ToString("F0"), 165, 369);
            DrawText(model.LabReportAttached ? "X" : string.Empty, 684, 510);
            DrawText(!model.LabReportAttached ? "X" : string.Empty, 752, 510);
            DrawText(model.VOCMethodNumber, 750, 525);
            DrawText(model.CertificationName, 140, 690);
            DrawText(model.CertificationTitle, 140, 708);
            DrawText(model.CertificationDate?.ToString("MM/dd/yyyy"), 140, 726);
            document.Save(outputStream, false);
        }

        outputStream.Position = 0;
        if (model.LabReportAttached && !string.IsNullOrWhiteSpace(model.VOCReportFileStoragePath))
        {
            var vocAbsolutePath = Path.Combine(_environment.WebRootPath, model.VOCReportFileStoragePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (System.IO.File.Exists(vocAbsolutePath))
            {
                using var mergedOutput = new MemoryStream();
                using var mergedDoc = new PdfDocument();
                using (var baseDoc = PdfReader.Open(outputStream, PdfDocumentOpenMode.Import))
                {
                    foreach (var page in baseDoc.Pages) mergedDoc.AddPage(page);
                }

                using (var vocDoc = PdfReader.Open(vocAbsolutePath, PdfDocumentOpenMode.Import))
                {
                    foreach (var page in vocDoc.Pages) mergedDoc.AddPage(page);
                }

                mergedDoc.Save(mergedOutput, false);
                return mergedOutput.ToArray();
            }
        }

        return outputStream.ToArray();
    }

    private async Task<byte[]> RenderNdar1PdfAsync(NDAR1 report, bool showGrid = false)
    {
        var templatePath = Path.Combine(_environment.WebRootPath, "forms", "Non-Discharge Application Report (NDAR-1) Form 131014.pdf");
        if (!System.IO.File.Exists(templatePath))
        {
            throw new Infrastructure.Exceptions.BusinessRuleException("NDAR-1 template PDF not found in wwwroot/forms.");
        }

        var irrigationReport = await _context.IrrRprts
            .Where(i => i.FacilityId == report.FacilityId &&
                        i.Month == report.Month &&
                        i.Year == report.Year)
            .OrderByDescending(i => i.UpdatedDate)
            .FirstOrDefaultAsync();
        var operatorLogsForMonth = await _context.OperatorLogs
            .Where(o => o.FacilityId == report.FacilityId &&
                        o.LogDate.Year == report.Year &&
                        o.LogDate.Month == (int)report.Month)
            .OrderBy(o => o.LogDate)
            .ThenBy(o => o.Id)
            .ToListAsync();
        var storageByDate = new Dictionary<DateTime, decimal?>();
        foreach (var log in operatorLogsForMonth)
        {
            var logDate = log.LogDate.Date;
            if (!storageByDate.ContainsKey(logDate))
            {
                storageByDate[logDate] = log.StorageFt;
            }
        }

        var map = BuildNdar1PdfMap();
        var allFields = report.Fields.OrderBy(f => f.FieldOrder).ToList();
        if (allFields.Count == 0)
        {
            throw new Infrastructure.Exceptions.BusinessRuleException("No NDAR-1 dynamic fields found for PDF export.");
        }

        var chunks = allFields
            .Select((field, idx) => new { field, idx })
            .GroupBy(x => x.idx / 4)
            .Select(g => g.Select(x => x.field).ToList())
            .ToList();

        using var output = new MemoryStream();
        using var document = new PdfDocument();
        var templateForm = XPdfForm.FromFile(templatePath);
        templateForm.PageNumber = 1;
        var pageCount = chunks.Count;

        for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            var chunk = chunks[pageIndex];
            var page = document.AddPage();
            page.Width = templateForm.PointWidth;
            page.Height = templateForm.PointHeight;

            var gfx = XGraphics.FromPdfPage(page);
            gfx.DrawImage(templateForm, 0, 0, page.Width, page.Height);

            var font = new XFont("Arial", 8, XFontStyle.Regular);
            var bold = new XFont("Arial", 8, XFontStyle.Bold);
            Draw(gfx, report.Facility?.PermitNumber, font, map.Header.PermitValue);
            Draw(gfx, report.Facility?.Name, font, map.Header.FacilityValue);
            Draw(gfx, report.Facility?.County, font, map.Header.CountyValue);
            Draw(gfx, new DateTime(report.Year, (int)report.Month, 1).ToString("MMMM"), font, map.Header.MonthValue);
            Draw(gfx, report.Year.ToString(), font, map.Header.YearValue);
            Draw(gfx, report.DidIrrigationOccur ? "X" : string.Empty, font, map.Header.IrrigationYes);
            Draw(gfx, report.DidIrrigationOccur ? string.Empty : "X", font, map.Header.IrrigationNo);
            Draw(gfx, (pageIndex + 1).ToString(), font, map.Header.PageNumber);
            Draw(gfx, pageCount.ToString(), font, map.Header.TotalPages);

            for (var i = 0; i < chunk.Count; i++)
            {
                var field = chunk[i];
                var x = map.FieldBlocks[i];
                Draw(gfx, field.Sprayfield?.FieldId ?? $"Field {field.FieldOrder}", font, new NdarPdfPoint(x.FieldNameX, map.FieldMetaY.FieldNameY));
                Draw(gfx, (field.Sprayfield != null ? SAM.Utilities.SprayfieldReportHelper.GetReportAcres(field.Sprayfield).ToString("F2") : string.Empty), font, new NdarPdfPoint(x.AreaX, map.FieldMetaY.AreaY));
                Draw(gfx, field.Sprayfield != null ? SAM.Utilities.SprayfieldZoneSummaryHelper.GetCropSummary(field.Sprayfield) ?? string.Empty : string.Empty, font, new NdarPdfPoint(x.CoverCropX, map.FieldMetaY.CoverCropY));
                Draw(gfx, field.Sprayfield?.HourlyRateInches?.ToString("F2"), font, new NdarPdfPoint(x.HourlyRateX, map.FieldMetaY.HourlyRateY));
                Draw(gfx, (field.Sprayfield?.AnnualRateInches ?? field.Sprayfield?.HydraulicLoadingLimitInPerYr)?.ToString("F2"), font, new NdarPdfPoint(x.AnnualRateX, map.FieldMetaY.AnnualRateY));
                var irrigated = field.DailyValues.Any(d => (d.TimeIrrigated ?? 0m) > 0m || (d.VolumeApplied ?? 0m) > 0m);
                Draw(gfx, irrigated ? "X" : string.Empty, font, new NdarPdfPoint(x.FieldIrrigatedYesX, map.FieldMetaY.FieldIrrigatedY));
                Draw(gfx, irrigated ? string.Empty : "X", font, new NdarPdfPoint(x.FieldIrrigatedNoX, map.FieldMetaY.FieldIrrigatedY));
            }

            var daysInMonth = Math.Min(DateTime.DaysInMonth(report.Year, (int)report.Month), map.Table.MaxRows);
            for (var day = 1; day <= daysInMonth; day++)
            {
                var dayIndex = day - 1;
                var rowY = map.Table.FirstRowY + (dayIndex * map.Table.RowHeight);
                Draw(gfx, SafeAt(report.WeatherCodeDaily, dayIndex), font, new NdarPdfPoint(map.Table.WeatherCodeX, rowY));
                Draw(gfx, FormatNumber(SafeAt(report.TemperatureDaily, dayIndex), "F0"), font, new NdarPdfPoint(map.Table.TemperatureX, rowY));
                Draw(gfx, FormatNumber(SafeAt(report.PrecipitationDaily, dayIndex), "0.##"), font, new NdarPdfPoint(map.Table.PrecipitationX, rowY));
                var date = new DateTime(report.Year, (int)report.Month, day);
                storageByDate.TryGetValue(date.Date, out var storageFt);
                Draw(gfx, FormatNumber(storageFt, "0.##"), font, new NdarPdfPoint(map.Table.StorageX, rowY));
                Draw(gfx, FormatNumber(SafeAt(report.FiveDayUpsetDaily, dayIndex), "0.##"), font, new NdarPdfPoint(map.Table.FiveDayUpsetX, rowY));

                for (var fieldIndex = 0; fieldIndex < chunk.Count; fieldIndex++)
                {
                    var field = chunk[fieldIndex];
                    var daily = field.DailyValues.FirstOrDefault(d => d.DayNo == day);
                    var col = map.FieldBlocks[fieldIndex];
                    Draw(gfx, FormatNumber(daily?.VolumeApplied, "F0"), font, new NdarPdfPoint(col.VolumeX, rowY));
                    Draw(gfx, FormatNumber(daily?.TimeIrrigated, "F0"), font, new NdarPdfPoint(col.TimeX, rowY));
                    Draw(gfx, FormatNumber(daily?.DailyLoading, "F2"), font, new NdarPdfPoint(col.DailyLoadingX, rowY));
                    Draw(gfx, FormatNumber(daily?.MaxHourlyLoading, "F2"), font, new NdarPdfPoint(col.MaxHourlyLoadingX, rowY));
                }
            }

            var monthlyY = map.Table.MonthlyY;
            var floatingY = map.Table.FloatingY;
            for (var fieldIndex = 0; fieldIndex < chunk.Count; fieldIndex++)
            {
                var field = chunk[fieldIndex];
                var col = map.FieldBlocks[fieldIndex];
                var monthlyVolumeSum = field.DailyValues
                    .Where(d => d.VolumeApplied.HasValue)
                    .Sum(d => Math.Round(d.VolumeApplied!.Value, 0, MidpointRounding.AwayFromZero));
                Draw(gfx, monthlyVolumeSum.ToString("F0"), font, new NdarPdfPoint(col.VolumeX, monthlyY));
                Draw(gfx, FormatNumber(field.MonthlyLoading, "F2"), font, new NdarPdfPoint(col.DailyLoadingX, monthlyY));
                Draw(gfx, FormatNumber(field.TwelveMonthFloatingTotal, "F2"), font, new NdarPdfPoint(col.DailyLoadingX, floatingY));
            }

            // Restore missing top border on the second-last footer cell block.
            gfx.DrawLine(new XPen(XColors.Black, 1.6), 730, 550, 770, 550);

            if (showGrid)
            {
                DrawCoordinateGrid(gfx, page.Width.Point, page.Height.Point);
            }
        }

        // Append template page 2 (Certification) and page 3 (Formulas) from the
        // original NDAR template so exports always include all 3 report sections.
        using var templateDoc = PdfReader.Open(templatePath, PdfDocumentOpenMode.Import);
        if (templateDoc.PageCount >= 2)
        {
            document.AddPage(templateDoc.Pages[1]);
            var certPage = document.Pages[document.PageCount - 1];
            var certGfx = XGraphics.FromPdfPage(certPage);
            var certFont = new XFont("Arial", 10, XFontStyle.Regular);
            Draw(certGfx, (chunks.Count + 1).ToString(), certFont, map.Certification.CertPageNumber);
            Draw(certGfx, (chunks.Count + 2).ToString(), certFont, map.Certification.CertTotalPages);

            var markCompliant = irrigationReport?.ComplianceStatus == SAM.Domain.Enums.ComplianceStatusEnum.Compliant;
            var markNonCompliant = irrigationReport?.ComplianceStatus == SAM.Domain.Enums.ComplianceStatusEnum.NonCompliant;

            Draw(certGfx, markCompliant ? "X" : string.Empty, certFont, map.Certification.Q1Compliant);
            Draw(certGfx, markCompliant ? "X" : string.Empty, certFont, map.Certification.Q2Compliant);
            Draw(certGfx, markCompliant ? "X" : string.Empty, certFont, map.Certification.Q3Compliant);
            Draw(certGfx, markCompliant ? "X" : string.Empty, certFont, map.Certification.Q4Compliant);
            Draw(certGfx, markCompliant ? "X" : string.Empty, certFont, map.Certification.Q5Compliant);

            Draw(certGfx, markNonCompliant ? "X" : string.Empty, certFont, map.Certification.Q1NonCompliant);
            Draw(certGfx, markNonCompliant ? "X" : string.Empty, certFont, map.Certification.Q2NonCompliant);
            Draw(certGfx, markNonCompliant ? "X" : string.Empty, certFont, map.Certification.Q3NonCompliant);
            Draw(certGfx, markNonCompliant ? "X" : string.Empty, certFont, map.Certification.Q4NonCompliant);
            Draw(certGfx, markNonCompliant ? "X" : string.Empty, certFont, map.Certification.Q5NonCompliant);

            Draw(certGfx, string.Empty, certFont, map.Certification.NonComplianceReasonStart);
            Draw(certGfx, string.IsNullOrWhiteSpace(report.CreatedBy) ? string.Empty : report.CreatedBy, certFont, map.Certification.OrcName);
            Draw(certGfx, string.Empty, certFont, map.Certification.OrcCertificationNo);
            Draw(certGfx, string.Empty, certFont, map.Certification.OrcGrade);
            Draw(certGfx, string.Empty, certFont, map.Certification.OrcPhone);
            Draw(certGfx, string.Empty, certFont, map.Certification.OrcChangedYes);
            Draw(certGfx, string.Empty, certFont, map.Certification.OrcChangedNo);
            Draw(certGfx, string.Empty, certFont, map.Certification.OrcSignature);
            Draw(certGfx, report.CreatedDate.ToString("MM/dd/yyyy"), certFont, map.Certification.OrcDate);

            Draw(certGfx, report.Facility?.Permittee, certFont, map.Certification.PermitteeName);
            Draw(certGfx, string.IsNullOrWhiteSpace(report.CreatedBy) ? string.Empty : report.CreatedBy, certFont, map.Certification.SigningOfficial);
            Draw(certGfx, "Authorized Agent", certFont, map.Certification.SigningOfficialTitle);
            Draw(certGfx, string.Empty, certFont, map.Certification.PermitteePhone);
            Draw(certGfx, report.Facility?.PermitExpirationDate?.ToString("MM/dd/yyyy"), certFont, map.Certification.PermitExp);
            Draw(certGfx, string.Empty, certFont, map.Certification.PermitteeSignature);
            Draw(certGfx, report.CreatedDate.ToString("MM/dd/yyyy"), certFont, map.Certification.PermitteeDate);

            if (showGrid)
            {
                DrawCoordinateGrid(certGfx, certPage.Width.Point, certPage.Height.Point);
            }
        }

        if (templateDoc.PageCount >= 3)
        {
            document.AddPage(templateDoc.Pages[2]);
            var formulaPage = document.Pages[document.PageCount - 1];
            var formulaGfx = XGraphics.FromPdfPage(formulaPage);
            if (showGrid)
            {
                DrawCoordinateGrid(formulaGfx, formulaPage.Width.Point, formulaPage.Height.Point);
            }
        }

        document.Save(output, false);
        return output.ToArray();
    }


    private static string SafeAt(IReadOnlyList<string?> values, int index)
        => index >= 0 && index < values.Count ? values[index] ?? string.Empty : string.Empty;

    private static decimal? SafeAt(IReadOnlyList<decimal?> values, int index)
        => index >= 0 && index < values.Count ? values[index] : null;

    private static string FormatNumber(decimal? value, string format)
        => value.HasValue ? value.Value.ToString(format) : string.Empty;

    private static void Draw(XGraphics gfx, string? text, XFont font, NdarPdfPoint point)
    {
        gfx.DrawString(text ?? string.Empty, font, XBrushes.Black, new XRect(point.X, point.Y, 220, font.Height + 2), XStringFormats.TopLeft);
    }

    private static Ndar1PdfMap BuildNdar1PdfMap()
    {
        return new Ndar1PdfMap(
            Header: new NdarHeaderMap(
                PermitValue: new NdarPdfPoint(80, 40),
                FacilityValue: new NdarPdfPoint(230, 40),
                CountyValue: new NdarPdfPoint(510, 40),
                MonthValue: new NdarPdfPoint(620, 40),
                YearValue: new NdarPdfPoint(730, 40),
                IrrigationYes: new NdarPdfPoint(43, 98),
                IrrigationNo: new NdarPdfPoint(90, 101),
                PageNumber: new NdarPdfPoint(683, 15.5),
                TotalPages: new NdarPdfPoint(720, 15.5)),
            FieldMetaY: new NdarFieldMetaYMap(
                FieldNameY: 56,
                AreaY: 72,
                CoverCropY: 85,
                HourlyRateY: 100,
                AnnualRateY: 113,
                FieldIrrigatedY: 122.5),
            Table: new NdarTableMap(
                MaxRows: 31,
                FirstRowY: 205,
                RowHeight: 11.18,
                WeatherCodeX: 40,
                TemperatureX: 69,
                PrecipitationX: 90,
                StorageX: 114,
                FiveDayUpsetX: 139,
                MonthlyY: 552,
                FloatingY: 563),
            FieldBlocks: new[]
            {
                new NdarFieldColumnMap(241, 241, 241, 241, 241, 170, 210, 250, 290, 242, 281),
                new NdarFieldColumnMap(395, 395, 395, 395, 395, 320, 360, 400, 440, 395, 433),
                new NdarFieldColumnMap(548, 548, 548, 548, 548, 470, 510, 550, 590, 546, 585),
                new NdarFieldColumnMap(695, 695, 695, 695, 695, 621, 661, 701, 741, 699, 738)
            },
            Certification: new NdarCertificationMap(
                CertPageNumber: new NdarPdfPoint(683, 15.5),
                CertTotalPages: new NdarPdfPoint(720, 15.5),
                Q1Compliant: new NdarPdfPoint(625, 44),
                Q2Compliant: new NdarPdfPoint(625, 67),
                Q3Compliant: new NdarPdfPoint(625, 89),
                Q4Compliant: new NdarPdfPoint(625, 111),
                Q5Compliant: new NdarPdfPoint(625, 133),
                Q1NonCompliant: new NdarPdfPoint(679, 44),
                Q2NonCompliant: new NdarPdfPoint(679, 67),
                Q3NonCompliant: new NdarPdfPoint(679, 89),
                Q4NonCompliant: new NdarPdfPoint(679, 111),
                Q5NonCompliant: new NdarPdfPoint(679, 133),
                NonComplianceReasonStart: new NdarPdfPoint(30, 180),
                OrcName: new NdarPdfPoint(50, 313),
                OrcCertificationNo: new NdarPdfPoint(100, 338),
                OrcGrade: new NdarPdfPoint(60, 360),
                OrcPhone: new NdarPdfPoint(200, 362),
                OrcChangedYes: new NdarPdfPoint(251, 386),
                OrcChangedNo: new NdarPdfPoint(287, 388),
                OrcSignature: new NdarPdfPoint(100, 410),
                OrcDate: new NdarPdfPoint(320, 420),
                PermitteeName: new NdarPdfPoint(448, 314),
                SigningOfficial: new NdarPdfPoint(460, 335),
                SigningOfficialTitle: new NdarPdfPoint(490, 360),
                PermitteePhone: new NdarPdfPoint(460, 382),
                PermitExp: new NdarPdfPoint(630, 382),
                PermitteeSignature: new NdarPdfPoint(490, 411),
                PermitteeDate: new NdarPdfPoint(700, 420)));
    }

    private sealed record NdarPdfPoint(double X, double Y);
    private sealed record Ndar1PdfMap(
        NdarHeaderMap Header,
        NdarFieldMetaYMap FieldMetaY,
        NdarTableMap Table,
        NdarFieldColumnMap[] FieldBlocks,
        NdarCertificationMap Certification);
    private sealed record NdarHeaderMap(
        NdarPdfPoint PermitValue,
        NdarPdfPoint FacilityValue,
        NdarPdfPoint CountyValue,
        NdarPdfPoint MonthValue,
        NdarPdfPoint YearValue,
        NdarPdfPoint IrrigationYes,
        NdarPdfPoint IrrigationNo,
        NdarPdfPoint PageNumber,
        NdarPdfPoint TotalPages);
    private sealed record NdarFieldMetaYMap(
        double FieldNameY,
        double AreaY,
        double CoverCropY,
        double HourlyRateY,
        double AnnualRateY,
        double FieldIrrigatedY);
    private sealed record NdarTableMap(
        int MaxRows,
        double FirstRowY,
        double RowHeight,
        double WeatherCodeX,
        double TemperatureX,
        double PrecipitationX,
        double StorageX,
        double FiveDayUpsetX,
        double MonthlyY,
        double FloatingY);
    private sealed record NdarFieldColumnMap(
        double FieldNameX,
        double AreaX,
        double CoverCropX,
        double HourlyRateX,
        double AnnualRateX,
        double VolumeX,
        double TimeX,
        double DailyLoadingX,
        double MaxHourlyLoadingX,
        double FieldIrrigatedYesX,
        double FieldIrrigatedNoX);

    private sealed record NdarCertificationMap(
        NdarPdfPoint CertPageNumber,
        NdarPdfPoint CertTotalPages,
        NdarPdfPoint Q1Compliant,
        NdarPdfPoint Q2Compliant,
        NdarPdfPoint Q3Compliant,
        NdarPdfPoint Q4Compliant,
        NdarPdfPoint Q5Compliant,
        NdarPdfPoint Q1NonCompliant,
        NdarPdfPoint Q2NonCompliant,
        NdarPdfPoint Q3NonCompliant,
        NdarPdfPoint Q4NonCompliant,
        NdarPdfPoint Q5NonCompliant,
        NdarPdfPoint NonComplianceReasonStart,
        NdarPdfPoint OrcName,
        NdarPdfPoint OrcCertificationNo,
        NdarPdfPoint OrcGrade,
        NdarPdfPoint OrcPhone,
        NdarPdfPoint OrcChangedYes,
        NdarPdfPoint OrcChangedNo,
        NdarPdfPoint OrcSignature,
        NdarPdfPoint OrcDate,
        NdarPdfPoint PermitteeName,
        NdarPdfPoint SigningOfficial,
        NdarPdfPoint SigningOfficialTitle,
        NdarPdfPoint PermitteePhone,
        NdarPdfPoint PermitExp,
        NdarPdfPoint PermitteeSignature,
        NdarPdfPoint PermitteeDate);

    private async Task<byte[]> RenderGw59APdfAsync(Gw59ExportModel model, bool showGrid = false)
    {
        var templatePath = Path.Combine(_environment.WebRootPath, "forms", "GW-59A.pdf");
        if (!System.IO.File.Exists(templatePath))
        {
            throw new Infrastructure.Exceptions.BusinessRuleException("GW-59A template PDF not found in wwwroot/forms.");
        }

        using var output = new MemoryStream();
        using (var document = new PdfDocument())
        {
            var templateForm = XPdfForm.FromFile(templatePath);
            templateForm.PageNumber = 1;

            var page = document.AddPage();
            page.Width = templateForm.PointWidth;
            page.Height = templateForm.PointHeight;

            var gfx = XGraphics.FromPdfPage(page);
            gfx.DrawImage(templateForm, 0, 0, page.Width, page.Height);
            var font = new XFont("Arial", 8, XFontStyle.Regular);
            var bold = new XFont("Arial", 8, XFontStyle.Bold);
            var map = BuildGw59ACalibrationMap();
            void Draw(string? text, double x, double y, bool isBold = false) =>
                gfx.DrawString(text ?? string.Empty, isBold ? bold : font, XBrushes.Black, new XRect(x, y, 320, 11), XStringFormats.TopLeft);
            void DrawMark(bool? value, bool yes, double x, double y)
            {
                if (value.HasValue && value.Value == yes)
                {
                    // Draw a tick as vector lines so it renders consistently in all viewers/fonts.
                    gfx.DrawLine(XPens.Black, x, y + 5, x + 3, y + 8);
                    gfx.DrawLine(XPens.Black, x + 3, y + 8, x + 9, y + 1);
                }
            }

            Draw(model.PermitNumber, map.PermitNumber.X, map.PermitNumber.Y, true);
            Draw(model.GW59ADueDate?.ToString("MM/dd/yyyy"), map.DueDate.X, map.DueDate.Y);

            DrawMark(model.GW59AQuestion1Response, true, map.Q1Yes.X, map.Q1Yes.Y);
            DrawMark(model.GW59AQuestion1Response, false, map.Q1No.X, map.Q1No.Y);
            DrawMark(model.GW59AQuestion2Response, true, map.Q2Yes.X, map.Q2Yes.Y);
            DrawMark(model.GW59AQuestion2Response, false, map.Q2No.X, map.Q2No.Y);
            DrawMark(model.GW59AQuestion3Response, true, map.Q3Yes.X, map.Q3Yes.Y);
            DrawMark(model.GW59AQuestion3Response, false, map.Q3No.X, map.Q3No.Y);
            DrawMark(model.GW59AQuestion4Response, true, map.Q4Yes.X, map.Q4Yes.Y);
            DrawMark(model.GW59AQuestion4Response, false, map.Q4No.X, map.Q4No.Y);
            DrawMark(model.GW59AQuestion5Response, true, map.Q5Yes.X, map.Q5Yes.Y);
            DrawMark(model.GW59AQuestion5Response, false, map.Q5No.X, map.Q5No.Y);
            DrawMark(model.GW59AQuestion6Response, true, map.Q6Yes.X, map.Q6Yes.Y);
            DrawMark(model.GW59AQuestion6Response, false, map.Q6No.X, map.Q6No.Y);
            DrawMark(model.GW59AQuestion7Response, true, map.Q7Yes.X, map.Q7Yes.Y);
            DrawMark(model.GW59AQuestion7Response, false, map.Q7No.X, map.Q7No.Y);

            Draw(model.GW59AQuestion2Details, map.Q2Details.X, map.Q2Details.Y);
            Draw(model.GW59AQuestion4Details, map.Q4Details.X, map.Q4Details.Y);
            Draw(model.GW59AQuestion5Details, map.Q5Details.X, map.Q5Details.Y);
            Draw(model.GW59AQuestion7Details, map.Q7Details.X, map.Q7Details.Y);

            Draw(model.GW59ASignerName, map.SignerName.X, map.SignerName.Y);
            Draw(model.GW59ASignedDate?.ToString("MM/dd/yyyy"), map.SignedDate.X, map.SignedDate.Y);

            if (showGrid)
            {
                DrawCoordinateGrid(gfx, page.Width.Point, page.Height.Point);
            }

            document.Save(output, false);
        }

        await Task.CompletedTask;
        return output.ToArray();
    }

    private static Gw59ACalibrationMap BuildGw59ACalibrationMap()
    {
        // Temporary calibration map sourced from the grid screenshot. Keep all coordinates centralized here.
        return new Gw59ACalibrationMap
        {
            PermitNumber = new Point2D(450, 30),
            DueDate = new Point2D(192, 78),

            Q1Yes = new Point2D(530, 80),
            Q1No = new Point2D(560, 80),
            Q2Yes = new Point2D(530, 112),
            Q2No = new Point2D(560, 113),
            Q3Yes = new Point2D(530, 190),
            Q3No = new Point2D(570, 190),
            Q4Yes = new Point2D(530, 220),
            Q4No = new Point2D(560, 220),
            Q5Yes = new Point2D(520, 310),
            Q5No = new Point2D(560, 310),
            Q6Yes = new Point2D(530, 429),
            Q6No = new Point2D(560, 429),
            Q7Yes = new Point2D(530, 510),
            Q7No = new Point2D(560, 510),

            Q2Details = new Point2D(70, 146),
            Q4Details = new Point2D(70, 265),
            Q5Details = new Point2D(70, 370),
            Q7Details = new Point2D(70, 586),

            SignerName = new Point2D(100, 710),
            SignedDate = new Point2D(410, 710)
        };
    }

    private sealed class Gw59ACalibrationMap
    {
        public Point2D PermitNumber { get; set; }
        public Point2D DueDate { get; set; }
        public Point2D Q1Yes { get; set; }
        public Point2D Q1No { get; set; }
        public Point2D Q2Yes { get; set; }
        public Point2D Q2No { get; set; }
        public Point2D Q3Yes { get; set; }
        public Point2D Q3No { get; set; }
        public Point2D Q4Yes { get; set; }
        public Point2D Q4No { get; set; }
        public Point2D Q5Yes { get; set; }
        public Point2D Q5No { get; set; }
        public Point2D Q6Yes { get; set; }
        public Point2D Q6No { get; set; }
        public Point2D Q7Yes { get; set; }
        public Point2D Q7No { get; set; }
        public Point2D Q2Details { get; set; }
        public Point2D Q4Details { get; set; }
        public Point2D Q5Details { get; set; }
        public Point2D Q7Details { get; set; }
        public Point2D SignerName { get; set; }
        public Point2D SignedDate { get; set; }
    }

    private readonly record struct Point2D(double X, double Y);

    private static void DrawCoordinateGrid(XGraphics gfx, double pageWidth, double pageHeight)
    {
        var gridPen = new XPen(XColor.FromArgb(220, 0, 102, 204), 0.5);
        var majorPen = new XPen(XColor.FromArgb(240, 220, 0, 0), 1.0);
        var labelFont = new XFont("Arial", 7, XFontStyle.Bold);
        var majorLabelFont = new XFont("Arial", 8, XFontStyle.Bold);

        for (int x = 0; x <= (int)pageWidth; x += 10)
        {
            var pen = x % 50 == 0 ? majorPen : gridPen;
            gfx.DrawLine(pen, x, 0, x, pageHeight);
            if (x % 50 == 0)
            {
                gfx.DrawString(x.ToString(), majorLabelFont, XBrushes.Red, new XRect(x + 1, 1, 24, 8), XStringFormats.TopLeft);
            }
            else if (x % 10 == 0 && x % 20 == 0)
            {
                gfx.DrawString(x.ToString(), labelFont, XBrushes.SteelBlue, new XRect(x + 1, 1, 20, 7), XStringFormats.TopLeft);
            }
        }

        for (int y = 0; y <= (int)pageHeight; y += 10)
        {
            var pen = y % 50 == 0 ? majorPen : gridPen;
            gfx.DrawLine(pen, 0, y, pageWidth, y);
            if (y % 50 == 0)
            {
                gfx.DrawString(y.ToString(), majorLabelFont, XBrushes.Red, new XRect(1, y + 1, 24, 8), XStringFormats.TopLeft);
            }
            else if (y % 10 == 0 && y % 20 == 0)
            {
                gfx.DrawString(y.ToString(), labelFont, XBrushes.SteelBlue, new XRect(1, y + 1, 20, 7), XStringFormats.TopLeft);
            }
        }
    }

    private static bool IsGw59AQuestionnaireEmpty(Gw59ExportModel model)
    {
        return !model.GW59ADueDate.HasValue
               && !model.GW59AQuestion1Response.HasValue
               && !model.GW59AQuestion2Response.HasValue
               && !model.GW59AQuestion3Response.HasValue
               && !model.GW59AQuestion4Response.HasValue
               && !model.GW59AQuestion5Response.HasValue
               && !model.GW59AQuestion6Response.HasValue
               && !model.GW59AQuestion7Response.HasValue
               && string.IsNullOrWhiteSpace(model.GW59AQuestion2Details)
               && string.IsNullOrWhiteSpace(model.GW59AQuestion4Details)
               && string.IsNullOrWhiteSpace(model.GW59AQuestion5Details)
               && string.IsNullOrWhiteSpace(model.GW59AQuestion7Details)
               && string.IsNullOrWhiteSpace(model.GW59ASignerName)
               && string.IsNullOrWhiteSpace(model.GW59ASignerTitle)
               && !model.GW59ASignedDate.HasValue;
    }

    #endregion
}
