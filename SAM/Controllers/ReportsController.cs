using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Azure.Storage.Blobs;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using SAM.Controllers.Base;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Domain.Extensions;
using SAM.Domain.Enums;
using SAM.Infrastructure.Authorization;
using SAM.Services.Helpers;
using SAM.Services.Interfaces;
using SAM.Services.Models;
using SAM.Utilities;
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
    private readonly IApplicationComplianceService _applicationComplianceService;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public ReportsController(
        IIrrRprtService irrRprtService,
        IFacilityService facilityService,
        INDAR1Service ndar1Service,
        INDAR1RowEditService ndar1RowEditService,
        ISprayfieldService sprayfieldService,
        INDMRService ndmrService,
        INDMLRService ndmlrService,
        IGWMonitService gwMonitService,
        IApplicationComplianceService applicationComplianceService,
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        IConfiguration configuration,
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
        _applicationComplianceService = applicationComplianceService;
        _context = context;
        _environment = environment;
        _configuration = configuration;
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
            report.WeatherSummary = viewModel.WeatherSummary ?? string.Empty;
            report.OperationalNotes = viewModel.OperationalNotes ?? string.Empty;
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

    [HttpGet]
    public async Task<IActionResult> NDMLRReports(
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
        var normalizedYear = year.HasValue && year.Value >= 2000 && year.Value <= 2100 ? year : null;

        var query = _context.NDMLRs
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

        if (normalizedYear.HasValue)
        {
            query = query.Where(r => r.Year == normalizedYear.Value);
        }

        query = normalizedSortBy switch
        {
            "facility" => normalizedSortDir == "asc"
                ? query.OrderBy(r => r.Facility != null ? r.Facility.Name : string.Empty).ThenByDescending(r => r.Year).ThenByDescending(r => r.Month).ThenByDescending(r => r.CreatedDate)
                : query.OrderByDescending(r => r.Facility != null ? r.Facility.Name : string.Empty).ThenByDescending(r => r.Year).ThenByDescending(r => r.Month).ThenByDescending(r => r.CreatedDate),
            _ => normalizedSortDir == "asc"
                ? query.OrderBy(r => r.Year).ThenBy(r => r.Month).ThenByDescending(r => r.CreatedDate)
                : query.OrderByDescending(r => r.Year).ThenByDescending(r => r.Month).ThenByDescending(r => r.CreatedDate)
        };

        var totalCount = await query.CountAsync();
        var reports = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync();

        var items = reports.Select(r => new NDMLRViewModel
        {
            Id = r.Id,
            CompanyId = r.CompanyId,
            CompanyName = r.Company?.Name,
            FacilityId = r.FacilityId,
            FacilityName = r.Facility?.Name,
            Month = r.Month,
            Year = r.Year,
            CreatedDate = r.CreatedDate,
            UpdatedDate = r.UpdatedDate,
            CreatedBy = r.CreatedBy
        }).ToList();

        var model = new NDMLRReportsIndexViewModel
        {
            IsGlobalAdmin = isGlobalAdmin,
            SelectedCompanyId = companyId,
            Facilities = await GetFacilitySelectListAsync(companyId),
            Filter = new NDMLRFilterViewModel
            {
                FacilityId = facilityId,
                Year = normalizedYear,
                Page = normalizedPage,
                PageSize = normalizedPageSize
            },
            Sort = new NDMLRSortViewModel
            {
                SortBy = normalizedSortBy,
                SortDir = normalizedSortDir
            },
            Reports = new PagedResult<NDMLRViewModel>
            {
                Items = items,
                TotalCount = totalCount,
                Page = normalizedPage,
                PageSize = normalizedPageSize
            },
            GenerateForm = new NDMLRCreateViewModel
            {
                CompanyId = companyId ?? Guid.Empty,
                FacilityId = facilityId ?? Guid.Empty,
                Month = (MonthEnum)DateTime.Now.Month,
                Year = normalizedYear ?? DateTime.Now.Year
            }
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> NDMLRReportDetails(Guid id)
    {
        var report = await _context.NDMLRs
            .Include(r => r.Company)
            .Include(r => r.Facility)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
        if (report == null)
        {
            return NotFound();
        }

        await EnsureCompanyAccessAsync(report.CompanyId);

        var model = new NDMLRDetailsViewModel
        {
            Id = report.Id,
            CompanyId = report.CompanyId,
            CompanyName = report.Company?.Name,
            FacilityId = report.FacilityId,
            FacilityName = report.Facility?.Name,
            Month = report.Month,
            Year = report.Year,
            CreatedDate = report.CreatedDate,
            UpdatedDate = report.UpdatedDate,
            CreatedBy = report.CreatedBy,
            Fields = await BuildNdmlrDetailFieldsAsync(report)
        };

        return View(model);
    }

    private async Task<IReadOnlyDictionary<(int Year, int Month), NdmlrPanChemistryInputs>> LoadNdmlrChemistryByMonthAsync(
        Guid facilityId,
        int startYear,
        int endYear)
    {
        var wwChars = await _context.WWChars
            .AsNoTracking()
            .Where(w => w.FacilityId == facilityId && w.Year >= startYear && w.Year <= endYear)
            .ToListAsync();

        var wwCharIds = wwChars.Select(w => w.Id).ToList();
        var templateValues = wwCharIds.Count == 0
            ? new List<WWCharTemplateValue>()
            : await _context.WWCharTemplateValues
                .AsNoTracking()
                .Where(v => wwCharIds.Contains(v.WWCharId))
                .ToListAsync();
        var templateParameterIds = templateValues.Select(v => v.FacilityPermitTemplateParameterId).Distinct().ToList();
        var pcsByTemplateParameterId = templateParameterIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _context.FacilityPermitTemplateParameters
                .AsNoTracking()
                .Include(x => x.PcsParameterCatalog)
                .Where(x => templateParameterIds.Contains(x.Id))
                .ToDictionaryAsync(
                    x => x.Id,
                    x => x.PcsParameterCatalog != null ? x.PcsParameterCatalog.PcsCode : string.Empty);

        return NdmlrExportCalculationHelper.BuildChemistryByMonth(
            wwChars,
            templateValues,
            pcsByTemplateParameterId);
    }

    private async Task<List<NDMLRFieldDetailsViewModel>> BuildNdmlrDetailFieldsAsync(NDMLR report)
    {
        var (windowStart, windowEnd) = GetNdmlrWindow(report.Year, report.Month);
        var monthKeys = BuildDescendingNdmlrWindowMonthKeys(report.Year, report.Month);
        var startYear = windowStart.Year;
        var startMonth = windowStart.Month;
        var endYear = windowEnd.Year;
        var endMonth = windowEnd.Month;

        var ndarReports = await _context.NDAR1s
            .Where(r => r.CompanyId == report.CompanyId &&
                        r.FacilityId == report.FacilityId &&
                        (r.Year > startYear || (r.Year == startYear && (int)r.Month >= startMonth)) &&
                        (r.Year < endYear || (r.Year == endYear && (int)r.Month <= endMonth)))
            .Include(r => r.Field1).ThenInclude(f => f!.Crop)
            .Include(r => r.Field2).ThenInclude(f => f!.Crop)
            .Include(r => r.Field3).ThenInclude(f => f!.Crop)
            .Include(r => r.Field4).ThenInclude(f => f!.Crop)
            .Include(r => r.Fields)
                .ThenInclude(f => f.Sprayfield)
                    .ThenInclude(s => s!.Crop)
            .Include(r => r.Fields)
                .ThenInclude(f => f.DailyValues)
            .AsNoTracking()
            .ToListAsync();

        var reportsByMonth = ndarReports
            .GroupBy(r => BuildMonthKey(r.Year, (int)r.Month))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedDate).First());

        var chemistryByMonth = await LoadNdmlrChemistryByMonthAsync(report.FacilityId, startYear, endYear);
        var facility = report.Facility
            ?? await _context.Facilities.AsNoTracking().FirstOrDefaultAsync(f => f.Id == report.FacilityId);
        var (mineralizationRate, volatilizationRate) = NdmlrExportCalculationHelper.ResolvePanRates(facility);

        var fieldMetaById = new Dictionary<Guid, (Sprayfield Sprayfield, int? PreferredOrder)>();

        foreach (var ndar in ndarReports)
        {
            if (ndar.Fields.Any())
            {
                foreach (var field in ndar.Fields.OrderBy(f => f.FieldOrder))
                {
                    if (field.Sprayfield == null)
                    {
                        continue;
                    }

                    var sprayfieldId = field.Sprayfield.Id;
                    if (!fieldMetaById.TryGetValue(sprayfieldId, out var existing))
                    {
                        fieldMetaById[sprayfieldId] = (field.Sprayfield, field.FieldOrder);
                        continue;
                    }

                    var selectedOrder = existing.PreferredOrder;
                    if (!selectedOrder.HasValue || field.FieldOrder < selectedOrder.Value)
                    {
                        fieldMetaById[sprayfieldId] = (existing.Sprayfield, field.FieldOrder);
                    }
                }

                continue;
            }

            foreach (var legacy in new[] { ndar.Field1, ndar.Field2, ndar.Field3, ndar.Field4 })
            {
                if (legacy == null || fieldMetaById.ContainsKey(legacy.Id))
                {
                    continue;
                }

                fieldMetaById[legacy.Id] = (legacy, null);
            }
        }

        var orderedFields = fieldMetaById.Values
            .OrderBy(x => x.PreferredOrder.HasValue ? 0 : 1)
            .ThenBy(x => x.PreferredOrder ?? int.MaxValue)
            .ThenBy(x => x.Sprayfield.FieldId)
            .Select(x => x.Sprayfield)
            .ToList();

        if (!orderedFields.Any())
        {
            return new List<NDMLRFieldDetailsViewModel>();
        }

        var monthlyVolumesByFieldByMonth = BuildMonthlyFieldVolumesByMonth(reportsByMonth);
        var monthlyLoadsByField = NdmlrExportCalculationHelper.BuildMonthlyLoadsByFieldAndMonth(
            orderedFields,
            monthKeys,
            monthlyVolumesByFieldByMonth,
            chemistryByMonth,
            mineralizationRate,
            volatilizationRate);

        var annualPanConcentrations = monthKeys
            .Select(monthKey =>
            {
                var (monthYear, monthNo) = ParseMonthKey(monthKey);
                return NdmlrExportCalculationHelper.TryGetMonthlyAveragePanMgL(
                    monthYear,
                    monthNo,
                    chemistryByMonth,
                    mineralizationRate,
                    volatilizationRate);
            })
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();
        var annualAvgConc = annualPanConcentrations.Count > 0 ? annualPanConcentrations.Average() : (decimal?)null;

        var annualVolumesByFieldId = new Dictionary<Guid, decimal>();
        foreach (var monthly in monthlyVolumesByFieldByMonth.Values)
        {
            foreach (var kvp in monthly)
            {
                if (!annualVolumesByFieldId.ContainsKey(kvp.Key))
                {
                    annualVolumesByFieldId[kvp.Key] = 0m;
                }

                annualVolumesByFieldId[kvp.Key] += kvp.Value;
            }
        }

        var result = new List<NDMLRFieldDetailsViewModel>();

        foreach (var field in orderedFields)
        {
            var annualVolume = annualVolumesByFieldId.TryGetValue(field.Id, out var volume) ? volume : 0m;
            var area = SprayfieldReportHelper.GetReportAcres(field);
            var annualLoad = monthlyLoadsByField.TryGetValue(field.Id, out var fieldLoads)
                ? fieldLoads.Values.Sum()
                : 0m;

            var metrics = await _applicationComplianceService.GetFieldRollingMetricsAsync(report.FacilityId, field.Id, windowEnd);

            result.Add(new NDMLRFieldDetailsViewModel
            {
                SprayfieldId = field.Id,
                FieldCode = field.FieldId ?? string.Empty,
                AreaAcres = area,
                CropSummary = SprayfieldZoneSummaryHelper.GetCropSummary(field) ?? string.Empty,
                AnnualVolumeGallons = annualVolume,
                AverageConcentrationMgL = annualAvgConc,
                AnnualLoadLbsPerAcre = annualLoad,
                PanFloatingLbsPerAcre = metrics.RollingPanLbsPerAcre,
                PanLimitLbsPerAcre = metrics.PanLimitLbsPerAcre,
                FieldLoaded = annualVolume > 0m
            });
        }

        return result;
    }

    private static Dictionary<int, Dictionary<Guid, decimal>> BuildMonthlyFieldVolumesByMonth(Dictionary<int, NDAR1> reportsByMonth)
    {
        var result = new Dictionary<int, Dictionary<Guid, decimal>>();

        foreach (var (month, report) in reportsByMonth)
        {
            var monthly = new Dictionary<Guid, decimal>();

            if (report.Fields.Any())
            {
                foreach (var field in report.Fields.OrderBy(f => f.FieldOrder))
                {
                    if (field.SprayfieldId == Guid.Empty)
                    {
                        continue;
                    }

                    var sum = field.DailyValues?.Sum(v => v.VolumeApplied ?? 0m) ?? 0m;
                    if (!monthly.ContainsKey(field.SprayfieldId))
                    {
                        monthly[field.SprayfieldId] = 0m;
                    }

                    monthly[field.SprayfieldId] += sum;
                }
            }
            else
            {
                AccumulateLegacyFieldVolume(monthly, report.Field1Id, report.Field1VolumeAppliedDaily);
                AccumulateLegacyFieldVolume(monthly, report.Field2Id, report.Field2VolumeAppliedDaily);
                AccumulateLegacyFieldVolume(monthly, report.Field3Id, report.Field3VolumeAppliedDaily);
                AccumulateLegacyFieldVolume(monthly, report.Field4Id, report.Field4VolumeAppliedDaily);
            }

            result[month] = monthly;
        }

        return result;
    }

    private static void AccumulateLegacyFieldVolume(Dictionary<Guid, decimal> monthly, Guid? sprayfieldId, List<decimal?> dailyVolumes)
    {
        if (!sprayfieldId.HasValue || sprayfieldId.Value == Guid.Empty)
        {
            return;
        }

        var sum = dailyVolumes?.Sum(v => v ?? 0m) ?? 0m;
        if (!monthly.ContainsKey(sprayfieldId.Value))
        {
            monthly[sprayfieldId.Value] = 0m;
        }

        monthly[sprayfieldId.Value] += sum;
    }

    private static int BuildMonthKey(int year, int month) => (year * 100) + month;

    private static (int Year, int Month) ParseMonthKey(int monthKey) => (monthKey / 100, monthKey % 100);

    private static List<int> BuildDescendingNdmlrWindowMonthKeys(int year, MonthEnum month)
    {
        var result = new List<int>(12);
        var cursor = new DateTime(year, (int)month, 1);
        for (var i = 0; i < 12; i++)
        {
            result.Add(BuildMonthKey(cursor.Year, cursor.Month));
            cursor = cursor.AddMonths(-1);
        }

        return result;
    }

    private static (DateTime WindowStart, DateTime WindowEnd) GetNdmlrWindow(int year, MonthEnum month)
    {
        var endMonthStart = new DateTime(year, (int)month, 1);
        var windowStart = endMonthStart.AddMonths(-11);
        var windowEnd = endMonthStart.AddMonths(1).AddDays(-1);
        return (windowStart, windowEnd);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> GenerateNDMLRReport(Guid? companyId = null, Guid? facilityId = null)
    {
        return RedirectToAction(nameof(NDMLRReports), new { companyId, facilityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> GenerateNDMLRReport(NDMLRCreateViewModel model)
    {
        await EnsureCompanyAccessAsync(model.CompanyId);

        if (!ModelState.IsValid)
        {
            var listResult = await NDMLRReports(
                companyId: model.CompanyId,
                facilityId: model.FacilityId,
                year: model.Year);

            if (listResult is ViewResult viewResult && viewResult.Model is NDMLRReportsIndexViewModel listModel)
            {
                listModel.GenerateForm = model;
                listModel.OpenGenerateModalOnLoad = true;
                return View(nameof(NDMLRReports), listModel);
            }

            return listResult;
        }

        var existing = await _context.NDMLRs
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.CompanyId == model.CompanyId &&
                x.FacilityId == model.FacilityId &&
                x.Year == model.Year &&
                x.Month == model.Month);

        if (existing != null)
        {
            TempData["ErrorMessage"] = $"An NDMLR for {model.Month} {model.Year} already exists for this facility.";
            return RedirectToAction(nameof(NDMLRReportDetails), new { id = existing.Id });
        }

        var entity = new NDMLR
        {
            CompanyId = model.CompanyId,
            FacilityId = model.FacilityId,
            Month = model.Month,
            Year = model.Year
        };

        _context.NDMLRs.Add(entity);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"NDMLR report generated successfully through {model.Month} {model.Year}.";
        return RedirectToAction(nameof(NDMLRReportDetails), new { id = entity.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> NDMLRReportDelete(Guid id)
    {
        NDMLR? report = null;
        try
        {
            report = await _context.NDMLRs
                .FirstOrDefaultAsync(x => x.Id == id);
            if (report == null)
            {
                return NotFound();
            }

            await EnsureCompanyAccessAsync(report.CompanyId);

            _context.NDMLRs.Remove(report);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "NDMLR report deleted successfully.";
            return RedirectToAction(nameof(NDMLRReports), new { facilityId = report.FacilityId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error deleting NDMLR report: {ex.Message}";
        }

        return RedirectToAction(nameof(NDMLRReports), new { facilityId = report?.FacilityId });
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var grid = await _ndar1RowEditService.BuildGridAsync(id, userId);
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
    public async Task<IActionResult> NDAR1BeginGridEdit(Guid ndar1Id)
    {
        var report = await _ndar1Service.GetByIdAsync(ndar1Id);
        if (report == null)
        {
            return NotFound();
        }

        await EnsureCompanyAccessAsync(report.CompanyId);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var userDisplay = User.FindFirstValue("FullName") ?? User.Identity?.Name ?? "User";
        var result = await _ndar1RowEditService.BeginGridEditAsync(ndar1Id, userId, userDisplay);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> NDAR1ReleaseGridEdit(Guid ndar1Id, [FromBody] NDAR1GridReleaseRequest request)
    {
        var report = await _ndar1Service.GetByIdAsync(ndar1Id);
        if (report == null)
        {
            return NotFound();
        }

        await EnsureCompanyAccessAsync(report.CompanyId);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _ndar1RowEditService.ReleaseGridEditAsync(ndar1Id, request?.LockTokens ?? new List<Guid>(), userId);
        return Ok(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> NDAR1RefreshGridTotals(Guid ndar1Id)
    {
        var report = await _ndar1Service.GetByIdAsync(ndar1Id);
        if (report == null)
        {
            return NotFound();
        }

        await EnsureCompanyAccessAsync(report.CompanyId);
        var result = await _ndar1RowEditService.GetGridFooterTotalsAsync(ndar1Id);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> NDAR1RefreshStoredReport(Guid ndar1Id)
    {
        try
        {
            var report = await _ndar1Service.GetByIdAsync(ndar1Id);
            if (report == null)
            {
                return NotFound();
            }

            await EnsureCompanyAccessAsync(report.CompanyId);
            var outcome = await _ndar1RowEditService.RefreshStoredReportAsync(ndar1Id);
            return Json(new
            {
                success = outcome.Updated || outcome.WasCreated,
                status = outcome.Status.ToString(),
                message = outcome.Message
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "NDAR1 stored report refresh failed for NDAR1 {Ndar1Id}", ndar1Id);
            return StatusCode(500, new
            {
                success = false,
                message = "We couldn't refresh the stored NDAR-1 report right now. Please try again."
            });
        }
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
        var ndmrReport = await _irrRprtService.GetByIdAsync(id);
        if (ndmrReport == null)
        {
            if (IsFetchRequest())
            {
                return NotFound(new ProblemDetails
                {
                    Title = "NDMR export failed",
                    Detail = "NDMR report not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            TempData["ErrorMessage"] = "NDMR report not found.";
            return RedirectToAction(nameof(NDMRReports));
        }

        await EnsureCompanyAccessAsync(ndmrReport.CompanyId);

        try
        {
            var excelBytes = await _ndmrService.ExportToExcelAsync(ndmrReport.Id);
            var fileName = $"NDMR_{ndmrReport.Facility?.Name}_{ndmrReport.Month}_{ndmrReport.Year}.xlsx";
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
            return RedirectToAction(nameof(NDMRReportDetails), new { id = ndmrReport.Id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportNDMRReportPdf(Guid id, bool showGrid = false)
    {
        var ndmrReport = await _irrRprtService.GetByIdAsync(id);
        if (ndmrReport == null)
        {
            if (IsFetchRequest())
            {
                return NotFound(new ProblemDetails
                {
                    Title = "NDMR PDF export failed",
                    Detail = "NDMR report not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            TempData["ErrorMessage"] = "NDMR report not found.";
            return RedirectToAction(nameof(NDMRReports));
        }

        await EnsureCompanyAccessAsync(ndmrReport.CompanyId);

        try
        {
            var pdfBytes = await RenderNdmrPdfAsync(ndmrReport, showGrid);
            var safeFacility = Regex.Replace(ndmrReport.Facility?.Name ?? "Facility", @"[^\w\-]+", "_");
            var fileName = $"NDMR_{safeFacility}_{ndmrReport.Month}_{ndmrReport.Year}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            if (IsFetchRequest())
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "NDMR PDF export failed",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            TempData["ErrorMessage"] = $"Error exporting NDMR PDF: {ex.Message}";
            return RedirectToAction(nameof(NDMRReportDetails), new { id = ndmrReport.Id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportNDMLRReport(Guid id)
    {
        var report = await _context.NDMLRs
            .Include(r => r.Facility)
            .FirstOrDefaultAsync(r => r.Id == id);
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
            return RedirectToAction(nameof(NDMLRReportDetails), new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportNDMLRReportPdf(Guid id, bool showGrid = false)
    {
        var report = await _context.NDMLRs
            .Include(r => r.Facility)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (report == null)
        {
            return NotFound();
        }

        await EnsureCompanyAccessAsync(report.CompanyId);

        try
        {
            var pdfBytes = await RenderNdmlrPdfAsync(report, showGrid);
            var safeFacility = Regex.Replace(report.Facility?.Name ?? "Facility", @"[^\w\-]+", "_");
            var fileName = $"NDMLR_{safeFacility}_{report.Month}_{report.Year}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            if (IsFetchRequest())
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "NDMLR PDF export failed",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            TempData["ErrorMessage"] = $"Error exporting NDMLR PDF: {ex.Message}";
            return RedirectToAction(nameof(NDMLRReportDetails), new { id });
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
        bool groupByDate = true,
        int page = 1,
        int pageSize = 25)
    {
        if (Request.Query.ContainsKey("groupByDate"))
        {
            groupByDate = Request.Query["groupByDate"]
                .Any(v => string.Equals(v, "true", StringComparison.OrdinalIgnoreCase));
        }

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

        var filter = new GroundwaterQualityReportsFilter
        {
            CompanyId = companyId,
            FacilityId = facilityId,
            MonitoringWellId = monitoringWellId,
            Month = normalizedMonth,
            Year = normalizedYear
        };

        var query = BuildGroundwaterQualityReportsQuery(filter);

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

        var recordCountsBySampleDate = new Dictionary<DateTime, int>();
        if (groupByDate)
        {
            var datesOnPage = rows.Select(r => r.SampleDate.Date).Distinct().ToList();
            if (datesOnPage.Count > 0)
            {
                var countQuery = BuildGroundwaterQualityReportsQuery(filter);
                var counts = await countQuery
                    .Where(x => datesOnPage.Contains(x.SampleDate.Date))
                    .GroupBy(x => x.SampleDate.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToListAsync();
                recordCountsBySampleDate = counts.ToDictionary(x => x.Date, x => x.Count);
            }
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
                PageSize = normalizedPageSize,
                GroupByDate = groupByDate
            },
            Sort = new GroundwaterQualitySortViewModel
            {
                SortBy = normalizedSortBy,
                SortDir = normalizedSortDir
            },
            RecordCountsBySampleDate = recordCountsBySampleDate,
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
            ContactPerson = model.ContactPerson,
            FacilityPhone = model.FacilityPhone,
            PermitExpirationDate = model.PermitExpirationDate,
            WellId = model.WellId,
            WellLocation = model.WellLocation,
            WellDepthFeet = model.WellDepthFeet,
            DiameterInches = model.DiameterInches,
            ScreenedIntervalFromFeet = model.ScreenedIntervalFromFeet,
            ScreenedIntervalToFeet = model.ScreenedIntervalToFeet,
            NumberOfWellsToBeSampled = model.NumberOfWellsToBeSampled,
            SampleDate = model.SampleDate,
            LabSampleAnalyzedDate = model.LabSampleAnalyzedDate,
            SampleDepth = model.SampleDepth,
            WaterLevel = model.WaterLevel,
            MeasuringPointAboveLandSurface = model.MeasuringPointAboveLandSurface,
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
            PHLab = model.PHLab,
            PhosphorusTotal = model.PhosphorusTotal,
            LabName = model.LabName,
            LabCertificationNumber = model.LabCertificationNumber,
            LabReportAttached = model.LabReportAttached,
            VOCMethodNumber = model.VOCMethodNumber,
            GwOperationLagoon = model.GwOperationLagoon,
            GwOperationSprayField = model.GwOperationSprayField,
            OtherParameterLines = model.OtherParameterLines,
            CertificationName = model.CertificationName,
            CertificationTitle = model.CertificationTitle,
            CertificationDate = model.CertificationDate,
            ParameterSnapshots = model.ParameterSnapshots
        };
        return View("~/Views/OperationalData/GWMonitReport.cshtml", preview);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> ExportGW59Report(Guid id, bool showGrid = false)
    {
        var model = await BuildGw59ExportModelAsync(id);
        var bytes = await RenderCombinedGw59PdfAsync(model, showGrid);
        var safeFacility = string.IsNullOrWhiteSpace(model.FacilityName) ? "Facility" : model.FacilityName.Replace(' ', '_');
        return File(bytes, "application/pdf", $"GW59_{safeFacility}_{model.SampleDate:yyyyMMdd}.pdf");
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> ExportGW59ReportsBySampleDate(
        DateTime sampleDate,
        Guid? companyId = null,
        Guid? facilityId = null,
        Guid? monitoringWellId = null,
        int? month = null,
        int? year = null,
        bool showGrid = false)
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

        var normalizedMonth = month.HasValue && month.Value >= 1 && month.Value <= 12 ? month : null;
        var normalizedYear = year.HasValue && year.Value >= 2000 && year.Value <= 2100 ? year : null;
        var targetDate = sampleDate.Date;

        var filter = new GroundwaterQualityReportsFilter
        {
            CompanyId = companyId,
            FacilityId = facilityId,
            MonitoringWellId = monitoringWellId,
            Month = normalizedMonth,
            Year = normalizedYear
        };

        var ids = await BuildGroundwaterQualityReportsQuery(filter)
            .Where(x => x.SampleDate.Date == targetDate)
            .OrderBy(x => x.SampleDate)
            .ThenBy(x => x.Facility!.Name)
            .ThenBy(x => x.MonitoringWell!.WellId)
            .Select(x => x.Id)
            .ToListAsync();

        if (ids.Count == 0)
        {
            throw new Infrastructure.Exceptions.BusinessRuleException(
                $"No groundwater monitoring records found for {targetDate:MM/dd/yyyy} with the selected filters.");
        }

        var bytes = await RenderBulkGw59PdfAsync(ids, showGrid);
        return File(bytes, "application/pdf", $"GW59_{targetDate:yyyyMMdd}_All.pdf");
    }

    private sealed class GroundwaterQualityReportsFilter
    {
        public Guid? CompanyId { get; set; }
        public Guid? FacilityId { get; set; }
        public Guid? MonitoringWellId { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
    }

    private IQueryable<GWMonit> BuildGroundwaterQualityReportsQuery(GroundwaterQualityReportsFilter filter)
    {
        var query = _context.GWMonits
            .Include(x => x.Facility)
            .Include(x => x.MonitoringWell)
            .AsNoTracking()
            .AsQueryable();

        if (filter.CompanyId.HasValue)
        {
            query = query.Where(x => x.CompanyId == filter.CompanyId.Value);
        }

        if (filter.FacilityId.HasValue)
        {
            query = query.Where(x => x.FacilityId == filter.FacilityId.Value);
        }

        if (filter.MonitoringWellId.HasValue)
        {
            query = query.Where(x => x.MonitoringWellId == filter.MonitoringWellId.Value);
        }

        if (filter.Month.HasValue)
        {
            query = query.Where(x => x.SampleDate.Month == filter.Month.Value);
        }

        if (filter.Year.HasValue)
        {
            query = query.Where(x => x.SampleDate.Year == filter.Year.Value);
        }

        return query;
    }

    private async Task<byte[]> RenderBulkGw59PdfAsync(IReadOnlyList<Guid> ids, bool showGrid = false)
    {
        using var assembledOutput = new MemoryStream();
        using var assembledDoc = new PdfDocument();

        foreach (var id in ids)
        {
            var model = await BuildGw59ExportModelAsync(id);
            var recordBytes = await RenderCombinedGw59PdfAsync(model, showGrid);
            ImportPdfBytes(assembledDoc, recordBytes);
        }

        assembledDoc.Save(assembledOutput, false);
        return assembledOutput.ToArray();
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
                SortOrder = x.FacilityPermitTemplateParameter!.SortOrder,
                PcsCode = x.FacilityPermitTemplateParameter!.PcsParameterCatalog?.PcsCode ?? string.Empty,
                ParameterName = x.FacilityPermitTemplateParameter.ParameterDisplayOverride
                    ?? x.FacilityPermitTemplateParameter.PcsParameterCatalog?.UserFriendlyName
                    ?? x.FacilityPermitTemplateParameter.PcsParameterCatalog?.OfficialParameterName
                    ?? string.Empty,
                Units = x.FacilityPermitTemplateParameter.UnitsOverride
                    ?? x.FacilityPermitTemplateParameter.PcsParameterCatalog?.AcceptedUnits
                    ?? string.Empty,
                Value = x.NumericValue,
                IsReportingDetectionLimit = x.IsReportingDetectionLimit,
                DailyMaximumLimit = x.FacilityPermitTemplateParameter.DailyMaximumLimit,
                IsGw59 = (x.FacilityPermitTemplateParameter.ReportTypes & PermitTemplateReportTypeEnum.Gw59) != 0,
                IsGw59A = (x.FacilityPermitTemplateParameter.ReportTypes & PermitTemplateReportTypeEnum.Gw59A) != 0
            })
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.PcsCode)
            .ToList();

        var hasGw59APermitTemplateRows = permit != null && await _context.FacilityPermitTemplateParameters
            .AsNoTracking()
            .AnyAsync(x => x.FacilityPermitId == permit.Id && (x.ReportTypes & PermitTemplateReportTypeEnum.Gw59A) != 0);

        var currentUser = await GetCurrentUserAsync();
        var chemistry = Gw59ChemistryResolver.Resolve(snapshots);
        var facility = gwMonit.Facility ?? await _context.Facilities
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == gwMonit.FacilityId);
        var preferredPermit = await Gw59FacilityFieldResolver.ResolvePreferredPermitAsync(_context, facility, permit);
        var exportPermit = preferredPermit ?? permit;
        var labOption = await Gw59FacilityFieldResolver.ResolveLabOptionAsync(
            _context,
            gwMonit.LabOptionId,
            facility?.CompanyId ?? gwMonit.CompanyId);
        var facilityWellCount = await Gw59FacilityFieldResolver.CountMonitoringWellsForFacilityAsync(
            _context,
            gwMonit.FacilityId,
            facility?.CompanyId ?? gwMonit.CompanyId);
        var wellsCount = Gw59FacilityFieldResolver.ResolveNumberOfWellsToBeSampled(facility, exportPermit, facilityWellCount);
        var labInfo = Gw59FacilityFieldResolver.ResolveLabInfo(facility, labOption);
        var wellLocation = await Gw59FacilityFieldResolver.ResolveWellLocationAsync(
            _context,
            gwMonit.MonitoringWellId,
            gwMonit.MonitoringWell);
        var wellData = Gw59WellDataFields.FromMonitoringWell(gwMonit.MonitoringWell);
        return new Gw59ExportModel
        {
            GwMonitId = gwMonit.Id,
            FacilityName = facility?.Name ?? string.Empty,
            PermitNumber = exportPermit?.PermitNumber ?? string.Empty,
            Permittee = facility?.Permittee ?? string.Empty,
            Address = Gw59FacilityFieldResolver.ResolveAddress(facility, exportPermit),
            City = Gw59FacilityFieldResolver.ResolveCity(facility, exportPermit),
            State = Gw59FacilityFieldResolver.ResolveState(facility, exportPermit),
            ZipCode = Gw59FacilityFieldResolver.ResolveZipCode(facility, exportPermit),
            County = Gw59FacilityFieldResolver.ResolveCounty(facility, exportPermit),
            ContactPerson = Gw59FacilityFieldResolver.ResolveContactPerson(facility),
            FacilityPhone = Gw59FacilityFieldResolver.ResolveFacilityPhone(facility),
            PermitExpirationDate = exportPermit?.EffectiveEndDate,
            WellId = gwMonit.MonitoringWell?.WellId ?? string.Empty,
            WellLocation = wellLocation,
            WellDepthFeet = wellData.WellDepthFeet,
            DiameterInches = wellData.DiameterInches,
            ScreenedIntervalFromFeet = wellData.ScreenedIntervalFromFeet,
            ScreenedIntervalToFeet = wellData.ScreenedIntervalToFeet,
            RelativeMpElevation = wellData.RelativeMpElevation,
            NumberOfWellsToBeSampled = wellsCount,
            SampleDate = gwMonit.SampleDate,
            LabSampleAnalyzedDate = gwMonit.LabSampleAnalyzedDate,
            SampleDepth = gwMonit.SampleDepth,
            WaterLevel = Gw59ChemistryResolver.ResolveWaterLevel(gwMonit.WaterLevel, snapshots),
            MeasuringPointAboveLandSurface = wellData.MeasuringPointAboveLandSurface,
            GallonsPumped = gwMonit.GallonsPumped,
            PHField = gwMonit.PH,
            TemperatureField = gwMonit.Temperature,
            SpecificConductance = gwMonit.Conductivity,
            Odor = gwMonit.Odor,
            Appearance = gwMonit.Appearance,
            MetalsUnfiltered = gwMonit.MetalsSamplesCollectedUnfiltered,
            MetalsAcidified = gwMonit.MetalSamplesFieldAcidified,
            TDS = chemistry.TDS,
            TOC = chemistry.TOC,
            Chloride = chemistry.Chloride,
            NH3N = chemistry.NH3N,
            NO3N = chemistry.NO3N,
            TKN = chemistry.TKN,
            Calcium = chemistry.Calcium,
            Magnesium = chemistry.Magnesium,
            FecalColiform = chemistry.FecalColiform,
            TotalColiform = chemistry.TotalColiform,
            PHLab = chemistry.PHLab,
            PhosphorusTotal = chemistry.PhosphorusTotal,
            LabName = labInfo.LabName,
            LabCertificationNumber = labInfo.LabCertificationNumber,
            CollectedBy = gwMonit.CollectedBy,
            AnalyzedBy = gwMonit.AnalyzedBy,
            LabReportAttached = gwMonit.VOCReportAttached.GetValueOrDefault(),
            VOCMethodNumber = gwMonit.VOCMethodNumber,
            CertificationName = string.IsNullOrWhiteSpace(currentUser?.FullName) ? (currentUser?.UserName ?? string.Empty) : currentUser!.FullName,
            CertificationTitle = "Authorized Agent",
            CertificationDate = DateTime.UtcNow.Date,
            VOCReportFileStoragePath = gwMonit.VOCReportFileStoragePath,
            HasGw59APermitTemplateRows = hasGw59APermitTemplateRows,
            GwOperationLagoon = exportPermit?.GwOperationLagoon ?? true,
            GwOperationSprayField = exportPermit?.GwOperationSprayField ?? true,
            OtherParameterLines = Gw59ChemistryResolver.ResolveOtherLines(snapshots).ToList(),
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
            GW59ASignerName = string.IsNullOrWhiteSpace(currentUser?.FullName) ? (currentUser?.UserName ?? string.Empty) : currentUser!.FullName,
            GW59ASignerTitle = "Authorized Agent",
            GW59ASignedDate = DateTime.UtcNow.Date
        };
    }

    private async Task<byte[]> RenderGw59PdfAsync(Gw59ExportModel model, bool showGrid = false)
    {
        var templatePath = Path.Combine(_environment.WebRootPath, "forms", Gw59PdfCalibration.TemplateFileName);
        if (!System.IO.File.Exists(templatePath))
        {
            throw new Infrastructure.Exceptions.BusinessRuleException("GW-59 template PDF not found in wwwroot/forms.");
        }

        using var outputStream = new MemoryStream();
        using (var document = PdfReader.Open(templatePath, PdfDocumentOpenMode.Modify))
        {
            var page = document.Pages[0];
            var gfx = XGraphics.FromPdfPage(page);
            var fields = Gw59PdfCalibration.Fields;
            var font = new XFont("Arial", 7, XFontStyle.Regular);
            var otherFont = new XFont("Arial", 6.5, XFontStyle.Regular);
            void DrawBaselineText(string? text, double x, double underlineY, double width, XFont drawFont)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return;
                }

                var baselineY = Gw59PdfCalibration.GetBaselineY(underlineY);
                var rect = new XRect(x, baselineY - drawFont.Height, width, drawFont.Height + 1);
                gfx.DrawString(text, drawFont, XBrushes.Black, rect, XStringFormats.BottomLeft);
            }

            void DrawFieldText(string? text, Gw59PdfTextSlot slot) =>
                DrawBaselineText(text, slot.X, slot.Y, slot.Width, font);

            void DrawCenteredFieldText(string? text, Gw59PdfTextSlot slot)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return;
                }

                var baselineY = Gw59PdfCalibration.GetBaselineY(slot.Y);
                var rect = new XRect(slot.X, baselineY - font.Height, slot.Width, font.Height + 1);
                gfx.DrawString(text, font, XBrushes.Black, rect, XStringFormats.BottomCenter);
            }

            void DrawFieldLabText(string? text, Gw59PdfUnderlineSlot slot) =>
                DrawBaselineText(text, slot.X, slot.Y, slot.Width, font);

            void DrawCheckboxMark(string? text, Gw59PdfTextSlot slot)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return;
                }

                DrawCheckboxX(gfx, slot.X, slot.Y, slot.Width);
            }

            void DrawMetalsMark(bool? value, bool yes, Gw59PdfTextSlot slot)
            {
                if (value.HasValue && value.Value == yes)
                {
                    DrawCheckboxX(gfx, slot.X, slot.Y, slot.Width);
                }
            }

            DrawFieldText(model.FacilityName, fields.FacilityName);
            DrawFieldText(model.PermitNumber, fields.PermitNumber);
            DrawFieldText(model.Permittee, fields.Permittee);
            DrawFieldText(model.Address, fields.Address);
            DrawFieldText(model.City, fields.City);
            DrawFieldText(model.ZipCode, fields.ZipCode);
            DrawFieldText(model.State, fields.State);
            DrawFieldText(model.County, fields.County);
            DrawFieldText(model.PermitExpirationDate?.ToString("MM/dd/yyyy"), fields.PermitExpirationDate);
            DrawFieldText(model.ContactPerson, fields.ContactPerson);
            DrawFieldText(model.FacilityPhone, fields.FacilityPhone);
            DrawFieldText(model.WellLocation, fields.WellLocation);
            DrawCenteredFieldText(model.NumberOfWellsToBeSampled?.ToString(), fields.NumberOfWellsToBeSampled);
            DrawFieldText(model.WellId, fields.WellId);
            DrawFieldText(model.SampleDate.ToString("MM/dd/yyyy"), fields.SampleDate);
            DrawFieldText(model.WellDepthFeet?.ToString("F2"), fields.WellDepthFeet);
            DrawFieldText(model.DiameterInches?.ToString("F2"), fields.DiameterInches);
            if (model.ScreenedIntervalFromFeet.HasValue || model.ScreenedIntervalToFeet.HasValue)
            {
                DrawFieldText(model.ScreenedIntervalFromFeet?.ToString("F2"), fields.ScreenedIntervalFromFeet);
                DrawFieldText(model.ScreenedIntervalToFeet?.ToString("F2"), fields.ScreenedIntervalToFeet);
            }
            DrawFieldText(model.RelativeMpElevation?.ToString("F2"), fields.RelativeMpElevation);
            DrawFieldText(model.WaterLevel?.ToString("F2"), fields.WaterLevel);
            DrawFieldText(model.MeasuringPointAboveLandSurface?.ToString("F2"), fields.MeasuringPointAboveLandSurface);
            DrawFieldText(model.GallonsPumped?.ToString("F2"), fields.GallonsPumped);
            DrawFieldText(model.PHField?.ToString("F2"), fields.PHField);
            DrawFieldText(model.TemperatureField?.ToString("F1"), fields.TemperatureField);
            DrawFieldText(model.SpecificConductance?.ToString("F2"), fields.SpecificConductance);
            DrawFieldText(model.Odor, fields.Odor);
            DrawFieldText(model.Appearance, fields.Appearance);

            DrawMetalsMark(model.MetalsUnfiltered, true, fields.MetalsUnfilteredYes);
            DrawMetalsMark(model.MetalsUnfiltered, false, fields.MetalsUnfilteredNo);
            DrawMetalsMark(model.MetalsAcidified, true, fields.MetalsAcidifiedYes);
            DrawMetalsMark(model.MetalsAcidified, false, fields.MetalsAcidifiedNo);
            DrawFieldLabText(model.LabSampleAnalyzedDate?.ToString("MM/dd/yyyy"), fields.LabSampleAnalyzedDate);
            DrawFieldLabText(model.LabName, fields.LabName);
            DrawFieldLabText(model.LabCertificationNumber, fields.LabCertificationNumber);

            var drawnNamedSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var snapshot in Gw59PdfCalibration.SelectNamedSnapshots(model.ParameterSnapshots))
            {
                if (!Gw59PdfCalibration.TryGetNamedSlot(snapshot.PcsCode, out var slot))
                {
                    continue;
                }

                var slotKey = Gw59PdfCalibration.GetSlotPositionKey(slot);
                if (!drawnNamedSlots.Add(slotKey))
                {
                    continue;
                }

                DrawBaselineText(
                    Gw59PdfCalibration.FormatSlotValue(snapshot.Value!.Value, slot, snapshot.IsReportingDetectionLimit),
                    slot.X,
                    slot.Y,
                    slot.Width,
                    font);
            }

            DrawCheckboxMark(model.LabReportAttached ? "X" : string.Empty, fields.LabReportAttachedYes);
            DrawCheckboxMark(!model.LabReportAttached ? "X" : string.Empty, fields.LabReportAttachedNo);
            DrawFieldLabText(model.VOCMethodNumber, fields.VOCMethodNumber);
            if (model.GwOperationLagoon)
            {
                DrawCheckboxX(gfx, fields.OperationLagoonTick.X, fields.OperationLagoonTick.Y, fields.OperationLagoonTick.Width);
            }

            if (model.GwOperationSprayField)
            {
                DrawCheckboxX(gfx, fields.OperationSprayFieldTick.X, fields.OperationSprayFieldTick.Y, fields.OperationSprayFieldTick.Width);
            }

            var otherLineIndex = 0;
            foreach (var otherLine in model.OtherParameterLines.Take(Gw59PdfCalibration.OtherLineLimit))
            {
                var otherSlot = Gw59PdfCalibration.GetOtherSlot(otherLineIndex++);
                DrawBaselineText(
                    Gw59PdfCalibration.FormatOtherLine(otherLine),
                    otherSlot.X,
                    otherSlot.Y,
                    otherSlot.Width,
                    otherFont);
            }

            if (showGrid)
            {
                DrawCoordinateGrid(gfx, page.Width.Point, page.Height.Point);
                DrawGw59OtherSlotGrid(gfx);
            }

            document.Save(outputStream, false);
        }

        return outputStream.ToArray();
    }

    private static void DrawGw59OtherSlotGrid(XGraphics gfx)
    {
        var markerFont = new XFont("Arial", 6, XFontStyle.Bold);
        var boxPen = new XPen(XColors.DarkGreen, 0.25);
        var otherFont = new XFont("Arial", 6.5, XFontStyle.Regular);
        for (var i = 0; i < Gw59PdfCalibration.OtherLineLimit; i++)
        {
            var slot = Gw59PdfCalibration.GetOtherSlot(i);
            var baselineY = Gw59PdfCalibration.GetBaselineY(slot.Y);
            var rect = new XRect(slot.X, baselineY - otherFont.Height, slot.Width, otherFont.Height + 1);
            gfx.DrawRectangle(boxPen, rect);
            gfx.DrawString(
                $"O{i + 1}",
                markerFont,
                XBrushes.DarkGreen,
                new XPoint(slot.X, baselineY),
                XStringFormats.BaseLineLeft);
        }
    }

    private async Task<byte[]> RenderCombinedGw59PdfAsync(Gw59ExportModel model, bool showGrid = false)
    {
        var baseGw59Bytes = await RenderGw59PdfAsync(model, showGrid);
        byte[]? gw59aBytes = null;

        if (!IsGw59AQuestionnaireEmpty(model))
        {
            gw59aBytes = await RenderGw59APdfAsync(model, showGrid);
        }

        using var assembledOutput = new MemoryStream();
        using (var assembledDoc = new PdfDocument())
        {
            ImportPdfBytes(assembledDoc, baseGw59Bytes);

            if (gw59aBytes != null)
            {
                ImportPdfBytes(assembledDoc, gw59aBytes);
            }

            if (model.LabReportAttached && !string.IsNullOrWhiteSpace(model.VOCReportFileStoragePath))
            {
                var container = await GetSamBlobContainerClientAsync();
                var vocBlobClient = container.GetBlobClient(model.VOCReportFileStoragePath);
                if (await vocBlobClient.ExistsAsync())
                {
                    try
                    {
                        await using var vocBuffer = await DownloadBlobToMemoryAsync(vocBlobClient);
                        using var vocDoc = PdfReader.Open(vocBuffer, PdfDocumentOpenMode.Import);
                        foreach (var page in vocDoc.Pages)
                        {
                            assembledDoc.AddPage(page);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Failed to merge VOC report PDF for GWMonit {GWMonitId}", model.GwMonitId);
                        throw new Infrastructure.Exceptions.BusinessRuleException(
                            "VOC report file could not be merged. Ensure the attached VOC report is a valid PDF.",
                            ex);
                    }
                }
            }

            assembledDoc.Save(assembledOutput, false);
        }

        return assembledOutput.ToArray();
    }

    private static void ImportPdfBytes(PdfDocument target, byte[] bytes)
    {
        using var sourceStream = new MemoryStream(bytes);
        using var sourceDoc = PdfReader.Open(sourceStream, PdfDocumentOpenMode.Import);
        foreach (var page in sourceDoc.Pages)
        {
            target.AddPage(page);
        }
    }

    private async Task<BlobContainerClient> GetSamBlobContainerClientAsync()
    {
        var connectionString = _configuration.GetConnectionString("StorageConnectionString");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("StorageConnectionString is not configured.");
        }

        var blobServiceClient = new BlobServiceClient(connectionString);
        var container = blobServiceClient.GetBlobContainerClient("sam-files");
        await container.CreateIfNotExistsAsync();
        return container;
    }

    private static async Task<MemoryStream> DownloadBlobToMemoryAsync(BlobClient blobClient)
    {
        var download = await blobClient.DownloadStreamingAsync();
        var buffer = new MemoryStream();
        await using var source = download.Value.Content;
        await source.CopyToAsync(buffer);
        buffer.Position = 0;
        return buffer;
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
        var facility = report.Facility
            ?? await _context.Facilities.AsNoTracking().FirstOrDefaultAsync(f => f.Id == report.FacilityId);
        var reportDate = new DateTime(report.Year, (int)report.Month, 1);
        var ndarPermit = await ResolvePermitForDateAsync(report.FacilityId, reportDate);
        var ndarExportPermit = await Gw59FacilityFieldResolver.ResolvePreferredPermitAsync(_context, facility, ndarPermit) ?? ndarPermit;
        var ndarHeaderPermitNumber = Gw59FacilityFieldResolver.ResolvePermitNumberForReport(facility, ndarExportPermit);
        var ndarHeaderCounty = Gw59FacilityFieldResolver.ResolveCounty(facility, ndarExportPermit);
        var ndarPermitExpiration = ndarExportPermit?.EffectiveEndDate;
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
        using var templateDoc = PdfReader.Open(templatePath, PdfDocumentOpenMode.Import);
        var pageCount = chunks.Count;
        var totalPages = pageCount * 2;
        var currentPageNumber = 1;
        var hasCertificationTemplate = templateDoc.PageCount >= 2;

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
            Draw(gfx, ndarHeaderPermitNumber, font, map.Header.PermitValue);
            Draw(gfx, report.Facility?.Name, font, map.Header.FacilityValue);
            Draw(gfx, ndarHeaderCounty, font, map.Header.CountyValue);
            Draw(gfx, new DateTime(report.Year, (int)report.Month, 1).ToString("MMMM"), font, map.Header.MonthValue);
            Draw(gfx, report.Year.ToString(), font, map.Header.YearValue);
            Draw(gfx, report.DidIrrigationOccur ? "X" : string.Empty, font, map.Header.IrrigationYes);
            Draw(gfx, report.DidIrrigationOccur ? string.Empty : "X", font, map.Header.IrrigationNo);
            Draw(gfx, currentPageNumber.ToString(), font, map.Header.PageNumber);
            Draw(gfx, totalPages.ToString(), font, map.Header.TotalPages);

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
            currentPageNumber++;

            if (hasCertificationTemplate)
            {
                document.AddPage(templateDoc.Pages[1]);
                var certPage = document.Pages[document.PageCount - 1];
                var certGfx = XGraphics.FromPdfPage(certPage);
                var certFont = new XFont("Arial", 8, XFontStyle.Regular);
                Draw(certGfx, currentPageNumber.ToString(), certFont, map.Header.PageNumber);
                Draw(certGfx, totalPages.ToString(), certFont, map.Header.TotalPages);

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
                Draw(certGfx, ndarPermitExpiration?.ToString("MM/dd/yyyy"), certFont, map.Certification.PermitExp);
                Draw(certGfx, string.Empty, certFont, map.Certification.PermitteeSignature);
                Draw(certGfx, report.CreatedDate.ToString("MM/dd/yyyy"), certFont, map.Certification.PermitteeDate);

                if (showGrid)
                {
                    DrawCoordinateGrid(certGfx, certPage.Width.Point, certPage.Height.Point);
                }
            }

            currentPageNumber++;
        }

        document.Save(output, false);
        return output.ToArray();
    }

    private async Task<byte[]> RenderNdmlrPdfAsync(NDMLR report, bool showGrid = false)
    {
        var templatePath = Path.Combine(_environment.WebRootPath, "forms", "Non-Discharge Mass Loading Report (NDMLR) Form 131014.pdf");
        if (!System.IO.File.Exists(templatePath))
        {
            throw new Infrastructure.Exceptions.BusinessRuleException("NDMLR template PDF not found in wwwroot/forms.");
        }

        var (windowStart, windowEnd) = GetNdmlrWindow(report.Year, report.Month);
        var monthKeys = BuildDescendingNdmlrWindowMonthKeys(report.Year, report.Month);
        var ndmlrFacility = report.Facility
            ?? await _context.Facilities.AsNoTracking().FirstOrDefaultAsync(f => f.Id == report.FacilityId);
        var ndmlrPermit = await ResolvePermitForDateAsync(report.FacilityId, windowEnd);
        var ndmlrExportPermit = await Gw59FacilityFieldResolver.ResolvePreferredPermitAsync(_context, ndmlrFacility, ndmlrPermit) ?? ndmlrPermit;
        var ndmlrHeaderPermitNumber = Gw59FacilityFieldResolver.ResolvePermitNumberForReport(ndmlrFacility, ndmlrExportPermit);
        var ndmlrHeaderCounty = Gw59FacilityFieldResolver.ResolveCounty(ndmlrFacility, ndmlrExportPermit);
        var ndmlrPermitExpiration = ndmlrExportPermit?.EffectiveEndDate;
        var startYear = windowStart.Year;
        var startMonth = windowStart.Month;
        var endYear = windowEnd.Year;
        var endMonth = windowEnd.Month;

        // Build rolling 12-month field universe and month data (same source logic used by NDMLR Excel path).
        var ndarReports = await _context.NDAR1s
            .Where(r => r.CompanyId == report.CompanyId &&
                        r.FacilityId == report.FacilityId &&
                        (r.Year > startYear || (r.Year == startYear && (int)r.Month >= startMonth)) &&
                        (r.Year < endYear || (r.Year == endYear && (int)r.Month <= endMonth)))
            .Include(r => r.Field1).ThenInclude(f => f!.Crop)
            .Include(r => r.Field2).ThenInclude(f => f!.Crop)
            .Include(r => r.Field3).ThenInclude(f => f!.Crop)
            .Include(r => r.Field4).ThenInclude(f => f!.Crop)
            .Include(r => r.Fields)
                .ThenInclude(f => f.Sprayfield)
                    .ThenInclude(s => s!.Crop)
            .Include(r => r.Fields)
                .ThenInclude(f => f.DailyValues)
            .AsNoTracking()
            .ToListAsync();

        var reportsByMonth = ndarReports
            .GroupBy(r => BuildMonthKey(r.Year, (int)r.Month))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedDate).First());

        var fieldMetaById = new Dictionary<Guid, (Sprayfield Sprayfield, int? PreferredOrder)>();
        foreach (var ndar in ndarReports)
        {
            if (ndar.Fields.Any())
            {
                foreach (var field in ndar.Fields.OrderBy(f => f.FieldOrder))
                {
                    if (field.Sprayfield == null)
                    {
                        continue;
                    }

                    var sprayfieldId = field.Sprayfield.Id;
                    if (!fieldMetaById.TryGetValue(sprayfieldId, out var existing))
                    {
                        fieldMetaById[sprayfieldId] = (field.Sprayfield, field.FieldOrder);
                        continue;
                    }

                    if (!existing.PreferredOrder.HasValue || field.FieldOrder < existing.PreferredOrder.Value)
                    {
                        fieldMetaById[sprayfieldId] = (existing.Sprayfield, field.FieldOrder);
                    }
                }
                continue;
            }

            foreach (var legacy in new[] { ndar.Field1, ndar.Field2, ndar.Field3, ndar.Field4 })
            {
                if (legacy == null || fieldMetaById.ContainsKey(legacy.Id))
                {
                    continue;
                }

                fieldMetaById[legacy.Id] = (legacy, null);
            }
        }

        var orderedFields = fieldMetaById.Values
            .OrderBy(x => x.PreferredOrder.HasValue ? 0 : 1)
            .ThenBy(x => x.PreferredOrder ?? int.MaxValue)
            .ThenBy(x => x.Sprayfield.FieldId)
            .Select(x => x.Sprayfield)
            .ToList();

        if (!orderedFields.Any())
        {
            throw new Infrastructure.Exceptions.BusinessRuleException("No NDAR-1 source fields found for this NDMLR year.");
        }

        var monthlyVolumesByFieldByMonth = BuildMonthlyFieldVolumesByMonth(reportsByMonth);
        var yearlyVolumeByFieldId = new Dictionary<Guid, decimal>();
        foreach (var monthVolumes in monthlyVolumesByFieldByMonth.Values)
        {
            foreach (var kvp in monthVolumes)
            {
                if (!yearlyVolumeByFieldId.ContainsKey(kvp.Key))
                {
                    yearlyVolumeByFieldId[kvp.Key] = 0m;
                }

                yearlyVolumeByFieldId[kvp.Key] += kvp.Value;
            }
        }

        var fieldChunks = orderedFields
            .Select((field, idx) => new { field, idx })
            .GroupBy(x => x.idx / 5)
            .Select(g => g.Select(x => x.field).ToList())
            .ToList();

        var chemistryByMonth = await LoadNdmlrChemistryByMonthAsync(report.FacilityId, startYear, endYear);
        var (mineralizationRate, volatilizationRate) = NdmlrExportCalculationHelper.ResolvePanRates(ndmlrFacility);
        var monthKeysAscending = monthKeys.OrderBy(k => k).ToList();
        var monthlyLoadsByField = NdmlrExportCalculationHelper.BuildMonthlyLoadsByFieldAndMonth(
            orderedFields,
            monthKeys,
            monthlyVolumesByFieldByMonth,
            chemistryByMonth,
            mineralizationRate,
            volatilizationRate);
        var cumulativeByField = new Dictionary<Guid, Dictionary<int, decimal>>();
        foreach (var field in orderedFields)
        {
            monthlyLoadsByField.TryGetValue(field.Id, out var loads);
            cumulativeByField[field.Id] = NdmlrExportCalculationHelper.BuildForwardCumulativeLoadsByMonthKey(
                monthKeysAscending,
                loads ?? new Dictionary<int, decimal>());
        }

        using var output = new MemoryStream();
        using var document = new PdfDocument();
        var templateForm = XPdfForm.FromFile(templatePath);
        templateForm.PageNumber = 1;
        using var templateDoc = PdfReader.Open(templatePath, PdfDocumentOpenMode.Import);
        var hasCertificationTemplate = templateDoc.PageCount >= 2;
        var totalPages = hasCertificationTemplate ? fieldChunks.Count * 2 : fieldChunks.Count;
        var currentPageNumber = 1;

        var fieldNameXs = new[] { 158d, 296.8d, 435d, 571d, 710d };
        var areaXs = new[] { 158d, 296.8d, 435d, 571d, 710d };
        var cropXs = new[] { 158d, 296.8d, 435d, 571d, 710d };
        var loadTypeXs = new[] { 158d, 296.8d, 436d, 571d, 710d };
        var footerValueXs = new[] { 157.5d, 294d, 432d, 570d, 709d };

        for (var chunkIndex = 0; chunkIndex < fieldChunks.Count; chunkIndex++)
        {
            var chunk = fieldChunks[chunkIndex];
            var page = document.AddPage();
            page.Width = templateForm.PointWidth;
            page.Height = templateForm.PointHeight;

            var gfx = XGraphics.FromPdfPage(page);
            gfx.DrawImage(templateForm, 0, 0, page.Width, page.Height);
            var font = new XFont("Arial", 8, XFontStyle.Regular);

            // Header
            Draw(gfx, ndmlrHeaderPermitNumber, font, new NdarPdfPoint(73, 42));
            Draw(gfx, report.Facility?.Name, font, new NdarPdfPoint(220, 42));
            Draw(gfx, ndmlrHeaderCounty, font, new NdarPdfPoint(480, 42));
            Draw(gfx, $"{report.Month}", font, new NdarPdfPoint(633, 42));
            Draw(gfx, report.Year.ToString(), font, new NdarPdfPoint(734, 42));
            Draw(gfx, currentPageNumber.ToString(), font, new NdarPdfPoint(685, 16));
            Draw(gfx, totalPages.ToString(), font, new NdarPdfPoint(720, 16));

            // Field metadata blocks (5 slots)
            for (var i = 0; i < 5; i++)
            {
                var field = i < chunk.Count ? chunk[i] : null;
                if (field == null)
                {
                    continue;
                }

                var baseX = fieldNameXs[i];
                Draw(gfx, field.FieldId, font, new NdarPdfPoint(fieldNameXs[i], 60));
                Draw(gfx, SprayfieldReportHelper.GetReportAcres(field).ToString("F2"), font, new NdarPdfPoint(areaXs[i], 77));
                Draw(gfx, SprayfieldZoneSummaryHelper.GetCropSummary(field) ?? string.Empty, font, new NdarPdfPoint(cropXs[i], 90));
                Draw(gfx, "Wastewater", font, new NdarPdfPoint(loadTypeXs[i], 104));

                var isLoaded = yearlyVolumeByFieldId.TryGetValue(field.Id, out var annualVol) && annualVol > 0m;
                var yesOffset = i == 1 ? 0d : i == 2 ? -1d : 1d;
                Draw(gfx, isLoaded ? "X" : string.Empty, font, new NdarPdfPoint(baseX + yesOffset, 118.5));
                Draw(gfx, isLoaded ? string.Empty : "X", font, new NdarPdfPoint(baseX + 32, 118.5));
            }

            // Monthly rows (selected month descending for rolling 12-month window)
            for (var rowIndex = 0; rowIndex < monthKeys.Count; rowIndex++)
            {
                var monthKey = monthKeys[rowIndex];
                var (monthYear, monthNo) = ParseMonthKey(monthKey);
                var rowY = 207d + (rowIndex * 11.6d);
                var monthDate = new DateTime(monthYear, monthNo, 1);
                Draw(gfx, monthDate.ToString("MMMM"), font, new NdarPdfPoint(28, rowY));

                var monthAvgConc = NdmlrExportCalculationHelper.TryGetMonthlyAveragePanMgL(
                    monthYear,
                    monthNo,
                    chemistryByMonth,
                    mineralizationRate,
                    volatilizationRate);
                monthlyVolumesByFieldByMonth.TryGetValue(monthKey, out var monthVolumes);

                for (var i = 0; i < 5; i++)
                {
                    var field = i < chunk.Count ? chunk[i] : null;
                    if (field == null)
                    {
                        continue;
                    }

                    var baseX = fieldNameXs[i];
                    var volX = baseX - 77d;
                    var concX = baseX - 34d;
                    var monthlyLoadX = baseX - 1d;
                    var cumulativeLoadX = baseX + 30d;

                    var volume = 0m;
                    if (monthVolumes != null && monthVolumes.TryGetValue(field.Id, out var mappedVolume))
                    {
                        volume = mappedVolume;
                    }

                    if (volume > 0m)
                    {
                        Draw(gfx, volume.ToString("N0"), font, new NdarPdfPoint(volX, rowY));
                    }

                    if (monthAvgConc.HasValue)
                    {
                        Draw(gfx, monthAvgConc.Value.ToString("F2"), font, new NdarPdfPoint(concX, rowY));
                    }

                    if (monthlyLoadsByField.TryGetValue(field.Id, out var fieldLoads) &&
                        fieldLoads.TryGetValue(monthKey, out var monthlyLoad))
                    {
                        Draw(gfx, monthlyLoad.ToString("F2"), font, new NdarPdfPoint(monthlyLoadX, rowY));
                    }

                    if (NdmlrExportCalculationHelper.TryGetCumulativeLoadForDisplay(field.Id, monthKey, cumulativeByField, out var cumulativeLoad))
                    {
                        Draw(gfx, cumulativeLoad.ToString("F2"), font, new NdarPdfPoint(cumulativeLoadX, rowY));
                    }
                }
            }

            // Footer values
            for (var i = 0; i < 5; i++)
            {
                var field = i < chunk.Count ? chunk[i] : null;
                if (field == null)
                {
                    continue;
                }

                var metrics = await _applicationComplianceService.GetFieldRollingMetricsAsync(report.FacilityId, field.Id, windowEnd);
                Draw(gfx, metrics.RollingPanLbsPerAcre.ToString("F2"), font, new NdarPdfPoint(footerValueXs[i], 350));
                if (metrics.PanLimitLbsPerAcre.HasValue)
                {
                    Draw(gfx, metrics.PanLimitLbsPerAcre.Value.ToString("F2"), font, new NdarPdfPoint(footerValueXs[i], 371.5));
                }
            }

            if (showGrid)
            {
                DrawCoordinateGrid(gfx, page.Width.Point, page.Height.Point);
            }
            currentPageNumber++;

            if (hasCertificationTemplate)
            {
                document.AddPage(templateDoc.Pages[1]);
                var certPage = document.Pages[document.PageCount - 1];
                var certGfx = XGraphics.FromPdfPage(certPage);
                var certFont = new XFont("Arial", 8, XFontStyle.Regular);
                var facility = report.Facility;
                var orcName = facility?.OrcName ?? string.Empty;
                var operatorNumber = facility?.OperatorNumber ?? string.Empty;
                var operatorGrade = facility?.OperatorGrade ?? string.Empty;
                var operatorPhone = facility?.OperatorPhone ?? string.Empty;
                var permittee = facility?.Permittee ?? string.Empty;
                var permitPhone = facility?.PermitPhone ?? string.Empty;

                Draw(certGfx, currentPageNumber.ToString(), certFont, new NdarPdfPoint(685, 16));
                Draw(certGfx, totalPages.ToString(), certFont, new NdarPdfPoint(720, 16));
                Draw(certGfx, orcName, certFont, new NdarPdfPoint(50, 315));
                Draw(certGfx, operatorNumber, certFont, new NdarPdfPoint(110, 338));
                Draw(certGfx, operatorGrade, certFont, new NdarPdfPoint(53, 362));
                Draw(certGfx, operatorPhone, certFont, new NdarPdfPoint(250, 362));
                Draw(certGfx, facility?.ChangeInOrc == true ? "X" : string.Empty, certFont, new NdarPdfPoint(313, 377));
                Draw(certGfx, facility?.ChangeInOrc == true ? string.Empty : "X", certFont, new NdarPdfPoint(340, 385));

                Draw(certGfx, permittee, certFont, new NdarPdfPoint(473, 315));
                Draw(certGfx, orcName, certFont, new NdarPdfPoint(499, 338));
                Draw(certGfx, operatorGrade, certFont, new NdarPdfPoint(523, 362));
                Draw(certGfx, permitPhone, certFont, new NdarPdfPoint(480, 386));
                Draw(certGfx, ndmlrPermitExpiration?.ToString("MM/dd/yyyy"), certFont, new NdarPdfPoint(650, 387));

                CoverNdmlrCertificationDateFields(certGfx);

                if (showGrid)
                {
                    DrawCoordinateGrid(certGfx, certPage.Width.Point, certPage.Height.Point);
                }
                currentPageNumber++;
            }
        }

        document.Save(output, false);
        return output.ToArray();
    }

    private static void CoverNdmlrCertificationDateFields(XGraphics gfx)
    {
        var coverBrush = new XSolidBrush(XColors.White);
        gfx.DrawRectangle(coverBrush, new XRect(330, 422, 75, 14));
        gfx.DrawRectangle(coverBrush, new XRect(680, 422, 75, 14));
    }

    private sealed class NdmrPdfParameter
    {
        public required string PcsCode { get; init; }
        public required string DisplayName { get; init; }
        public required string Units { get; init; }
        public required string SampleType { get; init; }
        public required string SampleFrequency { get; init; }
        public required string MonthlyLimitText { get; init; }
        public required string DailyLimitText { get; init; }
        public required List<decimal?> DailyValues { get; init; }
        public required List<bool> DailyIsReportingDetectionLimit { get; init; }
        public decimal? Average { get; init; }
        public decimal? DailyMaximum { get; init; }
        public decimal? DailyMinimum { get; init; }
    }

    private sealed class NdmrPdfSnapshot
    {
        public required Facility Facility { get; init; }
        public required string ResolvedPermitNumber { get; init; }
        public required string ResolvedCounty { get; init; }
        public DateTime? ResolvedPermitExpiration { get; init; }
        public required string ResolvedLabName { get; init; }
        public required string ResolvedLabName2 { get; init; }
        public required string SamplingPerson1 { get; init; }
        public required string SamplingPerson2 { get; init; }
        public required MonthEnum Month { get; init; }
        public required int Year { get; init; }
        public required int DaysInMonth { get; init; }
        public required List<TimeSpan?> OrcArrivalByDay { get; init; }
        public required List<decimal?> OrcTimeOnSiteByDay { get; init; }
        public required List<List<NdmrPdfParameter>> ParameterChunks { get; init; }
        public NdmrPdfParameter? FlowParameter { get; init; }
        public ComplianceStatusEnum? ComplianceStatus { get; init; }
        public required string PpiLabel { get; init; }
        public FlowMeasuringPointEnum? FlowMeasuringPoint { get; init; }
        public ParameterMonitoringPointEnum? ParameterMonitoringPoint { get; init; }
    }

    private async Task<byte[]> RenderNdmrPdfAsync(IrrRprt report, bool showGrid = false)
    {
        _ = report ?? throw new ArgumentNullException(nameof(report));
        var templatePath = Path.Combine(_environment.WebRootPath, "forms", "Non-Discharge Monitoring Report (NDMR) Form 0312.pdf");
        if (!System.IO.File.Exists(templatePath))
        {
            throw new Infrastructure.Exceptions.BusinessRuleException("NDMR template PDF not found in wwwroot/forms.");
        }

        var snapshot = await BuildNdmrPdfSnapshotAsync(report);

        using var output = new MemoryStream();
        using var document = new PdfDocument();
        using var templateDoc = PdfReader.Open(templatePath, PdfDocumentOpenMode.Import);

        var hasCertificationTemplate = templateDoc.PageCount > 1;
        var totalPages = snapshot.ParameterChunks.Count + (hasCertificationTemplate ? 1 : 0);
        var font = new XFont("Arial", 8, XFontStyle.Regular);
        var boldFont = new XFont("Arial", 8, XFontStyle.Bold);
        var tinyBold = new XFont("Arial", 7, XFontStyle.Bold);

        var currentPage = 1;
        var parameterColumns = NdmrPdfCalibration.ParameterGrid.DynamicColumns;
        var flowColumn = NdmrPdfCalibration.ParameterGrid.FlowColumn;
        const double day1Y = 154.7d;
        const double dayRowHeight = 11.66d;
        const double orcArrivalX = 39d;
        const double orcArrivalWidth = 36d;
        const double orcTimeOnSiteX = 78d;
        const double orcTimeOnSiteWidth = 30d;
        // Footer rows continue the daily grid rhythm (row 32+).
        var footerStartY = day1Y + (31 * dayRowHeight);
        var footerRowHeight = dayRowHeight;
        var footerRowStep = dayRowHeight;
        var avgY = footerStartY;
        var maxY = footerStartY + footerRowStep;
        var minY = footerStartY + (2d * footerRowStep);
        var sampleTypeY = footerStartY + (3d * footerRowStep);
        var monthlyLimitY = footerStartY + (4d * footerRowStep);
        var dailyLimitY = footerStartY + (5d * footerRowStep);
        var sampleFreqY = footerStartY + (6d * footerRowStep);

        var chunkIndex = 0;
        foreach (var chunk in snapshot.ParameterChunks)
        {
            document.AddPage(templateDoc.Pages[0]);
            var page = document.Pages[document.PageCount - 1];
            var gfx = XGraphics.FromPdfPage(page);

            Draw(gfx, snapshot.ResolvedPermitNumber, boldFont, new NdarPdfPoint(73, 41));
            Draw(gfx, snapshot.Facility.Name, boldFont, new NdarPdfPoint(226, 41));
            Draw(gfx, snapshot.ResolvedCounty, boldFont, new NdarPdfPoint(482, 41));
            Draw(gfx, snapshot.Month.ToString(), boldFont, new NdarPdfPoint(608.5, 41));
            Draw(gfx, snapshot.Year.ToString(), boldFont, new NdarPdfPoint(721, 41));
            Draw(gfx, currentPage.ToString(), font, new NdarPdfPoint(685, 16));
            Draw(gfx, totalPages.ToString(), font, new NdarPdfPoint(720, 16));

            if (chunkIndex == 0)
            {
                DrawNdmrHeader(gfx, snapshot, boldFont);
            }

            if (chunkIndex == 0 && snapshot.FlowParameter is { } flow)
            {
                DrawFlowColumn(
                    gfx,
                    flow,
                    flowColumn,
                    font,
                    boldFont,
                    avgY,
                    maxY,
                    minY,
                    sampleTypeY,
                    monthlyLimitY,
                    dailyLimitY,
                    sampleFreqY,
                    snapshot.DaysInMonth,
                    day1Y,
                    dayRowHeight,
                    footerRowHeight);
            }

            for (var slot = 0; slot < chunk.Count && slot < parameterColumns.Count; slot++)
            {
                var p = chunk[slot];
                var col = parameterColumns[slot];
                DrawInCell(gfx, p.PcsCode, boldFont, col.Left, col.CodeY, col.Width, bold: true);
                DrawVerticalInCell(
                    gfx,
                    FormatVerticalColumnLabel(p.DisplayName),
                    tinyBold,
                    col.Left,
                    col.NameTopY,
                    col.Width,
                    col.NameHeight);
                DrawInCell(gfx, p.Units, boldFont, col.Left, col.UnitsY, col.Width, bold: true);
                DrawInCell(gfx, p.SampleType, font, col.Left, sampleTypeY, col.Width, cellHeight: footerRowHeight, verticalCenter: true);
                DrawInCell(gfx, p.MonthlyLimitText, font, col.Left, monthlyLimitY, col.Width, cellHeight: footerRowHeight, verticalCenter: true);
                DrawInCell(gfx, p.DailyLimitText, font, col.Left, dailyLimitY, col.Width, cellHeight: footerRowHeight, verticalCenter: true);
                DrawInCell(gfx, p.SampleFrequency, font, col.Left, sampleFreqY, col.Width, cellHeight: footerRowHeight, verticalCenter: true);
                DrawInCell(gfx, p.Average?.ToString("0.00"), font, col.Left, avgY, col.Width, cellHeight: footerRowHeight, verticalCenter: true);
                DrawInCell(gfx, p.DailyMaximum?.ToString("0.00"), font, col.Left, maxY, col.Width, cellHeight: footerRowHeight, verticalCenter: true);
                DrawInCell(gfx, p.DailyMinimum?.ToString("0.00"), font, col.Left, minY, col.Width, cellHeight: footerRowHeight, verticalCenter: true);
            }

            for (var day = 1; day <= snapshot.DaysInMonth; day++)
            {
                var y = day1Y + ((day - 1) * dayRowHeight);
                DrawInCell(gfx, snapshot.OrcArrivalByDay[day - 1]?.ToString(@"hh\:mm"), font, orcArrivalX, y, orcArrivalWidth, cellHeight: dayRowHeight, verticalCenter: true);
                DrawInCell(gfx, snapshot.OrcTimeOnSiteByDay[day - 1]?.ToString("0.00"), font, orcTimeOnSiteX, y, orcTimeOnSiteWidth, cellHeight: dayRowHeight, verticalCenter: true);

                for (var slot = 0; slot < chunk.Count && slot < parameterColumns.Count; slot++)
                {
                    var dayValue = chunk[slot].DailyValues[day - 1];
                    var isRdl = ReportingDetectionLimitHelper.IsRdlAtIndex(chunk[slot].DailyIsReportingDetectionLimit, day - 1);
                    var col = parameterColumns[slot];
                    var displayValue = dayValue.HasValue
                        ? ReportingDetectionLimitHelper.FormatDisplayValue(dayValue.Value, isRdl, "0.00")
                        : null;
                    DrawInCell(gfx, displayValue, font, col.Left, y, col.Width, cellHeight: dayRowHeight, verticalCenter: true);
                }
            }

            if (showGrid)
            {
                DrawCoordinateGrid(gfx, page.Width.Point, page.Height.Point);
            }
            currentPage++;
            chunkIndex++;
        }

        if (hasCertificationTemplate)
        {
            document.AddPage(templateDoc.Pages[1]);
            var certPage = document.Pages[document.PageCount - 1];
            var certGfx = XGraphics.FromPdfPage(certPage);
            var cert = NdmrPdfCalibration.Certification;

            Draw(certGfx, currentPage.ToString(), font, Point(cert.PageNumber));
            Draw(certGfx, totalPages.ToString(), font, Point(cert.TotalPages));

            Draw(certGfx, snapshot.SamplingPerson1, font, Point(cert.SamplingPerson1));
            Draw(certGfx, snapshot.SamplingPerson2, font, Point(cert.SamplingPerson2));
            Draw(certGfx, snapshot.ResolvedLabName, font, Point(cert.CertifiedLab1));
            Draw(certGfx, snapshot.ResolvedLabName2, font, Point(cert.CertifiedLab2));

            var markCompliant = snapshot.ComplianceStatus == ComplianceStatusEnum.Compliant;
            var markNonCompliant = snapshot.ComplianceStatus == ComplianceStatusEnum.NonCompliant;
            DrawCheckbox(certGfx, markCompliant, cert.Compliant);
            DrawCheckbox(certGfx, markNonCompliant, cert.NonCompliant);

            Draw(certGfx, snapshot.Facility.OrcName, font, Point(cert.OrcName));
            Draw(certGfx, snapshot.Facility.OperatorNumber, font, Point(cert.OrcCertificationNo));
            Draw(certGfx, snapshot.Facility.OperatorGrade, font, Point(cert.OrcGrade));
            Draw(certGfx, snapshot.Facility.OperatorPhone, font, Point(cert.OrcPhone));
            Draw(certGfx, snapshot.Facility.ChangeInOrc == true ? "X" : string.Empty, boldFont, Point(cert.OrcChangedYes));
            Draw(certGfx, snapshot.Facility.ChangeInOrc == true ? string.Empty : "X", boldFont, Point(cert.OrcChangedNo));

            Draw(certGfx, snapshot.Facility.Permittee, font, Point(cert.Permittee));
            Draw(certGfx, snapshot.Facility.OrcName, font, Point(cert.SigningOfficial));
            Draw(certGfx, snapshot.Facility.OperatorGrade, font, Point(cert.SigningOfficialTitle));
            Draw(certGfx, snapshot.Facility.PermitPhone, font, Point(cert.PermitteePhone));
            Draw(certGfx, snapshot.ResolvedPermitExpiration?.ToString("MM/dd/yyyy"), font, Point(cert.PermitExpiration));

            if (showGrid)
            {
                DrawCoordinateGrid(certGfx, certPage.Width.Point, certPage.Height.Point);
            }
        }

        document.Save(output, false);
        return output.ToArray();
    }

    private async Task<NdmrPdfSnapshot> BuildNdmrPdfSnapshotAsync(IrrRprt report)
    {
        var facility = report.Facility
            ?? await _context.Facilities.AsNoTracking().FirstOrDefaultAsync(f => f.Id == report.FacilityId)
            ?? throw new Infrastructure.Exceptions.BusinessRuleException("Facility not found for this NDMR export.");

        var monthNumber = (int)report.Month;
        var year = report.Year;
        var startDate = new DateTime(year, monthNumber, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var daysInMonth = DateTime.DaysInMonth(year, monthNumber);
        var permit = await ResolvePermitForDateAsync(report.FacilityId, startDate);
        var wwChar = await _context.WWChars
            .AsNoTracking()
            .FirstOrDefaultAsync(w =>
                w.FacilityId == report.FacilityId &&
                (int)w.Month == monthNumber &&
                w.Year == year);

        var labOption = await Gw59FacilityFieldResolver.ResolveLabOptionAsync(
            _context,
            wwChar?.LabOptionId,
            facility.CompanyId);
        var secondaryLabOption = await Gw59FacilityFieldResolver.ResolveLabOptionAsync(
            _context,
            wwChar?.SecondaryLabOptionId,
            facility.CompanyId);
        var labInfo = Gw59FacilityFieldResolver.ResolveLabInfo(facility, labOption);
        var secondaryLabInfo = Gw59FacilityFieldResolver.ResolveLabInfo(facility, secondaryLabOption);
        var exportPermit = await Gw59FacilityFieldResolver.ResolvePreferredPermitAsync(_context, facility, permit) ?? permit;

        var permitTemplateRows = permit == null
            ? new List<FacilityPermitTemplateParameter>()
            : await _context.FacilityPermitTemplateParameters
                .AsNoTracking()
                .Include(x => x.PcsParameterCatalog)
                .Where(x => x.FacilityPermitId == permit.Id && (x.ReportTypes & PermitTemplateReportTypeEnum.Ndmr) != 0)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.CreatedDate)
                .ThenBy(x => x.Id)
                .ToListAsync();
        permitTemplateRows = permitTemplateRows
            .Where(row => IsNdmrTemplateRowApplicableForMonth(row, monthNumber))
            .ToList();

        if (permitTemplateRows.Any() &&
            !permitTemplateRows.Any(x => string.Equals(x.PcsParameterCatalog?.PcsCode, "50050", StringComparison.OrdinalIgnoreCase)))
        {
            throw new Infrastructure.Exceptions.BusinessRuleException("Permit template must include PCS code 50050 (Flow) for NDMR export.");
        }

        var wwCharTemplateValues = wwChar == null
            ? new List<WWCharTemplateValue>()
            : await _context.WWCharTemplateValues
                .AsNoTracking()
                .Where(x => x.WWCharId == wwChar.Id && x.DayNo >= 1 && x.DayNo <= 31)
                .ToListAsync();
        var wwDailyValueByKey = wwCharTemplateValues
            .GroupBy(x => (x.FacilityPermitTemplateParameterId, x.DayNo))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedDate).First().NumericValue);
        var wwDailyRdlByKey = wwCharTemplateValues
            .GroupBy(x => (x.FacilityPermitTemplateParameterId, x.DayNo))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedDate).First().IsReportingDetectionLimit);

        var operatorLogs = await _context.OperatorLogs
            .AsNoTracking()
            .Where(o => o.FacilityId == report.FacilityId && o.LogDate >= startDate && o.LogDate <= endDate)
            .OrderByDescending(o => o.UpdatedDate ?? o.CreatedDate)
            .ThenByDescending(o => o.CreatedDate)
            .ToListAsync();

        var gwMonits = await _context.GWMonits
            .AsNoTracking()
            .Where(g => g.FacilityId == report.FacilityId && g.SampleDate >= startDate && g.SampleDate <= endDate)
            .ToListAsync();

        var flowTemplateRow = permitTemplateRows.FirstOrDefault(row =>
            string.Equals(row.PcsParameterCatalog?.PcsCode, "50050", StringComparison.OrdinalIgnoreCase));
        NdmrPdfParameter? flowParameter = flowTemplateRow == null
            ? null
            : BuildNdmrPdfParameter(
                flowTemplateRow,
                wwDailyValueByKey,
                wwDailyRdlByKey,
                wwChar,
                gwMonits,
                year,
                monthNumber,
                daysInMonth);

        var parameters = permitTemplateRows
            .Select(row =>
            {
                var code = row.PcsParameterCatalog?.PcsCode ?? string.Empty;
                if (string.IsNullOrWhiteSpace(code)) return null;
                if (string.Equals(code, "50050", StringComparison.OrdinalIgnoreCase)) return null;
                return BuildNdmrPdfParameter(
                    row,
                    wwDailyValueByKey,
                    wwDailyRdlByKey,
                    wwChar,
                    gwMonits,
                    year,
                    monthNumber,
                    daysInMonth);
            })
            .Where(x => x != null)
            .Cast<NdmrPdfParameter>()
            .ToList();

        if (parameters.Count == 0)
        {
            parameters = new List<NdmrPdfParameter>();
        }

        var chunks = parameters
            .Select((p, idx) => new { p, idx })
            .GroupBy(x => x.idx / 15)
            .Select(g => g.Select(x => x.p).ToList())
            .ToList();
        if (chunks.Count == 0)
        {
            chunks.Add(new List<NdmrPdfParameter>());
        }

        var orcArrival = Enumerable.Repeat<TimeSpan?>(null, 31).ToList();
        var orcTimeOnSite = Enumerable.Repeat<decimal?>(null, 31).ToList();
        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, monthNumber, day).Date;
            var dayLogs = operatorLogs.Where(o => o.LogDate.Date == date).ToList();
            if (dayLogs.Count == 0) continue;

            var firstLog = dayLogs.OrderBy(o => o.ArrivalTime).First();
            var canonicalLog = dayLogs
                .OrderByDescending(o => o.UpdatedDate ?? o.CreatedDate)
                .ThenByDescending(o => o.CreatedDate)
                .First();
            orcArrival[day - 1] = firstLog.ArrivalTime;
            orcTimeOnSite[day - 1] = canonicalLog.TimeOnSiteHours;
        }

        return new NdmrPdfSnapshot
        {
            Facility = facility,
            ResolvedPermitNumber = Gw59FacilityFieldResolver.ResolvePermitNumberForReport(facility, exportPermit),
            ResolvedCounty = Gw59FacilityFieldResolver.ResolveCounty(facility, exportPermit),
            ResolvedPermitExpiration = exportPermit?.EffectiveEndDate,
            ResolvedLabName = labInfo.LabName,
            ResolvedLabName2 = secondaryLabInfo.LabName,
            SamplingPerson1 = wwChar?.SamplingPerson1 ?? string.Empty,
            SamplingPerson2 = wwChar?.SamplingPerson2 ?? string.Empty,
            Month = report.Month,
            Year = year,
            DaysInMonth = daysInMonth,
            OrcArrivalByDay = orcArrival,
            OrcTimeOnSiteByDay = orcTimeOnSite,
            ParameterChunks = chunks,
            FlowParameter = flowParameter,
            ComplianceStatus = report.ComplianceStatus,
            PpiLabel = "002",
            FlowMeasuringPoint = wwChar?.FlowMeasuringPoint,
            ParameterMonitoringPoint = wwChar?.ParameterMonitoringPoint
        };
    }

    private static NdmrPdfParameter BuildNdmrPdfParameter(
        FacilityPermitTemplateParameter row,
        IReadOnlyDictionary<(Guid FacilityPermitTemplateParameterId, int DayNo), decimal?> wwDailyValueByKey,
        IReadOnlyDictionary<(Guid FacilityPermitTemplateParameterId, int DayNo), bool> wwDailyRdlByKey,
        WWChar? wwChar,
        IReadOnlyList<GWMonit> gwMonits,
        int year,
        int monthNumber,
        int daysInMonth)
    {
        var code = row.PcsParameterCatalog?.PcsCode ?? string.Empty;
        var name = row.ParameterDisplayOverride
            ?? row.PcsParameterCatalog?.UserFriendlyName
            ?? row.PcsParameterCatalog?.OfficialParameterName;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"PCS {code}";
        }

        var units = NdmrFlowFormatting.IsFlowPcs(code)
            ? NdmrFlowFormatting.FlowUnitsLabel
            : row.UnitsOverride ?? row.PcsParameterCatalog?.AcceptedUnits ?? string.Empty;

        var daily = new List<decimal?>(31);
        var dailyRdl = new List<bool>(31);
        for (var day = 1; day <= daysInMonth; day++)
        {
            decimal? value = null;
            var isRdl = false;
            if (wwDailyValueByKey.TryGetValue((row.Id, day), out var wwValue))
            {
                value = wwValue;
                wwDailyRdlByKey.TryGetValue((row.Id, day), out isRdl);
            }

            value ??= ResolveNdmrPdfFallbackDailyValue(
                code,
                new DateTime(year, monthNumber, day),
                wwChar,
                gwMonits);
            daily.Add(value);
            dailyRdl.Add(isRdl);
        }

        for (var day = daysInMonth + 1; day <= 31; day++)
        {
            daily.Add(null);
            dailyRdl.Add(false);
        }

        var days = daily.Take(daysInMonth).Where(v => v.HasValue).Select(v => v!.Value).ToList();
        decimal? average = ReportingDetectionLimitHelper.AverageForReporting(daily, dailyRdl, code);
        if (days.Count > 0)
        {
            if (NdmrFlowFormatting.IsFlowPcs(code))
            {
                var mgdDays = days.Select(v => NdmrFlowFormatting.NormalizeFlowValueToMgd(v)!.Value).ToList();
                return new NdmrPdfParameter
                {
                    PcsCode = code,
                    DisplayName = name ?? string.Empty,
                    Units = units,
                    SampleType = row.SampleType.ToString(),
                    SampleFrequency = row.MeasurementFrequency.ToDisplayLabel(),
                    MonthlyLimitText = NdmrFlowFormatting.BuildFlowMonthlyLimitText(row),
                    DailyLimitText = row.DailyMaximumLimit.HasValue
                        ? NdmrFlowFormatting.FormatFlowLimit(row.DailyMaximumLimit)
                        : row.DailyMinimumLimit.HasValue
                            ? NdmrFlowFormatting.FormatFlowLimit(row.DailyMinimumLimit)
                            : string.Empty,
                    DailyValues = daily,
                    DailyIsReportingDetectionLimit = dailyRdl,
                    Average = mgdDays.Average(),
                    DailyMaximum = mgdDays.Max(),
                    DailyMinimum = mgdDays.Min()
                };
            }
        }

        return new NdmrPdfParameter
        {
            PcsCode = code,
            DisplayName = name ?? string.Empty,
            Units = units,
            SampleType = row.SampleType.ToString(),
            SampleFrequency = row.MeasurementFrequency.ToDisplayLabel(),
            MonthlyLimitText = NdmrFlowFormatting.IsFlowPcs(code)
                ? NdmrFlowFormatting.BuildFlowMonthlyLimitText(row)
                : row.MonthlyAverageLimit?.ToString("0.##")
                    ?? row.MonthlyGeometricMeanLimit?.ToString("0.##")
                    ?? string.Empty,
            DailyLimitText = row.DailyMaximumLimit?.ToString("0.##")
                ?? row.DailyMinimumLimit?.ToString("0.##")
                ?? string.Empty,
            DailyValues = daily,
            DailyIsReportingDetectionLimit = dailyRdl,
            Average = average,
            DailyMaximum = days.Count > 0 ? days.Max() : null,
            DailyMinimum = days.Count > 0 ? days.Min() : null
        };
    }

    private static void DrawFlowColumn(
        XGraphics gfx,
        NdmrPdfParameter flow,
        NdmrParameterColumnSlot col,
        XFont font,
        XFont boldFont,
        double avgY,
        double maxY,
        double minY,
        double sampleTypeY,
        double monthlyLimitY,
        double dailyLimitY,
        double sampleFreqY,
        int daysInMonth,
        double day1Y,
        double dayRowHeight,
        double footerRowHeight)
    {
        DrawFlowUnitsLabel(gfx, col, boldFont);
        DrawInCell(gfx, flow.SampleType, font, col.Left, sampleTypeY, col.Width, cellHeight: footerRowHeight, verticalCenter: true);
        DrawInCell(gfx, flow.MonthlyLimitText, font, col.Left, monthlyLimitY, col.Width, cellHeight: footerRowHeight, verticalCenter: true);
        DrawInCell(gfx, flow.DailyLimitText, font, col.Left, dailyLimitY, col.Width, cellHeight: footerRowHeight, verticalCenter: true);
        DrawInCell(gfx, flow.SampleFrequency, font, col.Left, sampleFreqY, col.Width, cellHeight: footerRowHeight, verticalCenter: true);
        DrawInCell(gfx, NdmrFlowFormatting.FormatFlowValue(flow.Average), font, col.Left, avgY, col.Width, cellHeight: footerRowHeight, verticalCenter: true);
        DrawInCell(gfx, NdmrFlowFormatting.FormatFlowValue(flow.DailyMaximum), font, col.Left, maxY, col.Width, cellHeight: footerRowHeight, verticalCenter: true);
        DrawInCell(gfx, NdmrFlowFormatting.FormatFlowValue(flow.DailyMinimum), font, col.Left, minY, col.Width, cellHeight: footerRowHeight, verticalCenter: true);

        for (var day = 1; day <= daysInMonth; day++)
        {
            var y = day1Y + ((day - 1) * dayRowHeight);
            var dayValue = flow.DailyValues[day - 1];
            DrawInCell(gfx, NdmrFlowFormatting.FormatFlowValue(dayValue), font, col.Left, y, col.Width, cellHeight: dayRowHeight, verticalCenter: true);
        }
    }

    private static void DrawFlowUnitsLabel(XGraphics gfx, NdmrParameterColumnSlot col, XFont boldFont)
    {
        var cover = NdmrPdfCalibration.FlowUnitsGpdCover;
        var shadeBrush = new XSolidBrush(XColor.FromArgb(
            255,
            NdmrPdfCalibration.FlowColumnShadeRed,
            NdmrPdfCalibration.FlowColumnShadeGreen,
            NdmrPdfCalibration.FlowColumnShadeBlue));
        gfx.DrawRectangle(shadeBrush, new XRect(cover.Left, cover.Top, cover.Width, cover.Height));
        var unitsY = col.UnitsY + NdmrPdfCalibration.FlowUnitsLabelOffsetY;
        DrawInCell(
            gfx,
            NdmrFlowFormatting.FlowUnitsLabel,
            boldFont,
            col.Left,
            unitsY,
            col.Width,
            cellHeight: cover.Top + cover.Height - unitsY,
            verticalCenter: true,
            bold: true);
    }

    private static void DrawNdmrHeader(
        XGraphics gfx,
        NdmrPdfSnapshot snapshot,
        XFont boldFont)
    {
        var header = NdmrPdfCalibration.Header;
        DrawInCell(
            gfx,
            snapshot.PpiLabel,
            boldFont,
            header.Ppi.X,
            header.HeaderRowTop,
            header.PpiWidth,
            cellHeight: header.HeaderRowHeight,
            verticalCenter: true,
            bold: true);

        DrawMonitoringPointCheckboxes(
            gfx,
            NdmrMonitoringPointOptions.BuildFlowOptions(snapshot.FlowMeasuringPoint),
            header.FlowOptions);

        DrawMonitoringPointCheckboxes(
            gfx,
            NdmrMonitoringPointOptions.BuildParameterOptions(snapshot.ParameterMonitoringPoint),
            header.ParameterOptions);
    }

    private static void DrawMonitoringPointCheckboxes(
        XGraphics gfx,
        IReadOnlyList<(string Text, bool Selected)> options,
        IReadOnlyList<NdmrPdfMonitoringOptionSlot> slots)
    {
        for (var i = 0; i < options.Count && i < slots.Count; i++)
        {
            if (!options[i].Selected)
            {
                continue;
            }

            var slot = slots[i];
            DrawNdmrCheckboxMark(gfx, slot);
        }
    }

    private static void DrawNdmrCheckboxMark(XGraphics gfx, NdmrPdfMonitoringOptionSlot slot)
    {
        const double half = 1.85d;
        var cx = slot.BoxLeft + (slot.BoxSize / 2d) + slot.MarkOffsetX;
        var cy = slot.BoxTop + (slot.BoxSize / 2d) + slot.MarkOffsetY;
        gfx.DrawLine(XPens.Black, cx - half, cy - half, cx + half, cy + half);
        gfx.DrawLine(XPens.Black, cx + half, cy - half, cx - half, cy + half);
    }

    private static decimal? ResolveNdmrPdfFallbackDailyValue(
        string pcsCode,
        DateTime currentDate,
        WWChar? wwChar,
        IReadOnlyList<GWMonit> gwMonits)
    {
        decimal? WwAt(IReadOnlyList<decimal?>? values)
        {
            if (values == null) return null;
            var idx = currentDate.Day - 1;
            return idx >= 0 && idx < values.Count ? values[idx] : null;
        }

        decimal? Avg(Func<GWMonit, decimal?> selector)
        {
            var values = gwMonits
                .Where(g => g.SampleDate.Date == currentDate.Date)
                .Select(selector)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();
            return values.Count == 0 ? null : values.Average();
        }

        return pcsCode switch
        {
            "50050" => WwAt(wwChar?.FlowRateDaily),
            "00310" => WwAt(wwChar?.BOD5Daily),
            "00620" => Avg(g => g.NO3N),
            "00610" => Avg(g => g.NH3N),
            "00625" => Avg(g => g.TKN),
            "00400" => Avg(g => g.PH),
            "31616" => Avg(g => g.FecalColiform),
            "00940" => Avg(g => g.Chloride),
            "00665" => Avg(g => g.TOC),
            "00530" => Avg(g => g.TSS),
            "00600" => Avg(g => (!g.TKN.HasValue && !g.NO3N.HasValue) ? null : (g.TKN ?? 0m) + (g.NO3N ?? 0m)),
            _ => null
        };
    }

    private static bool IsNdmrTemplateRowApplicableForMonth(FacilityPermitTemplateParameter row, int month)
    {
        var alwaysInclude = row.MeasurementFrequency is MeasurementFrequencyEnum.Daily
            or MeasurementFrequencyEnum.Weekly
            or MeasurementFrequencyEnum.Monthly
            or MeasurementFrequencyEnum.Continuous;
        if (alwaysInclude)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(row.ScheduledMonthsCsv))
        {
            return false;
        }

        var months = row.ScheduledMonthsCsv
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(value => int.TryParse(value, out var parsed) ? parsed : -1);
        return months.Any(m => m == month);
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

    private static NdarPdfPoint Point(NdmrPdfTextSlot slot) => new(slot.X, slot.Y);

    private static void DrawCheckbox(XGraphics gfx, bool marked, NdmrPdfTextSlot slot)
    {
        if (!marked || slot.BoxSize is not { } boxSize)
        {
            return;
        }

        DrawCheckboxX(gfx, slot.X, slot.Y, boxSize);
    }

    private static void DrawInCell(
        XGraphics gfx,
        string? text,
        XFont font,
        double x,
        double y,
        double width,
        double? cellHeight = null,
        bool bold = false,
        bool verticalCenter = false)
    {
        var value = text ?? string.Empty;
        var height = cellHeight ?? (font.Height + 2);

        if (verticalCenter)
        {
            gfx.DrawString(
                value,
                font,
                XBrushes.Black,
                new XRect(x, y, width, height),
                XStringFormats.Center);
            return;
        }

        gfx.DrawString(
            value,
            font,
            XBrushes.Black,
            new XRect(x, y, width, height),
            XStringFormats.TopCenter);
    }

    private static void DrawVerticalInCell(
        XGraphics gfx,
        string? text,
        XFont font,
        double cellX,
        double cellY,
        double cellWidth,
        double cellHeight)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var words = text
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return;
        }

        const double segmentGap = 0.5d;
        const double minFontSize = 5.5d;
        var maxRowWidth = Math.Max(8d, cellWidth - 2d);
        var maxBlockHeight = Math.Max(8d, cellHeight - 2d);

        static double WordPitch(XFont drawFont) => drawFont.Height * 1.05d;

        static double WordVerticalExtent(XGraphics g, string word, XFont drawFont) =>
            g.MeasureString(word, drawFont).Width;

        static double RowHorizontalSpan(IReadOnlyList<string> row, XFont drawFont)
        {
            const double gap = 0.5d;
            return (row.Count * WordPitch(drawFont)) + (gap * Math.Max(0, row.Count - 1));
        }

        static double RowVerticalExtent(XGraphics g, IReadOnlyList<string> row, XFont drawFont) =>
            row.Max(word => WordVerticalExtent(g, word, drawFont));

        static double LayoutVerticalExtent(XGraphics g, IReadOnlyList<List<string>> rows, XFont drawFont) =>
            rows.Max(row => RowVerticalExtent(g, row, drawFont));

        static List<List<string>> PackRows(IReadOnlyList<string> parts, XFont drawFont, double rowWidthLimit)
        {
            const double gap = 0.5d;
            var packed = new List<List<string>>();
            var row = new List<string>();
            var rowWidth = 0d;

            foreach (var part in parts)
            {
                var pitch = WordPitch(drawFont);
                var addGap = row.Count > 0 ? gap : 0d;
                if (row.Count > 0 && rowWidth + addGap + pitch > rowWidthLimit)
                {
                    packed.Add(row);
                    row = new List<string>();
                    rowWidth = 0d;
                    addGap = 0d;
                }

                row.Add(part);
                rowWidth += addGap + pitch;
            }

            if (row.Count > 0)
            {
                packed.Add(row);
            }

            return packed;
        }

        static IEnumerable<List<List<string>>> ContiguousPartitions(string[] parts, int rowCount)
        {
            if (rowCount <= 0 || rowCount > parts.Length)
            {
                yield break;
            }

            if (rowCount == 1)
            {
                yield return new List<List<string>> { parts.ToList() };
                yield break;
            }

            foreach (var split in BuildPartitionIndices(parts.Length, rowCount))
            {
                var rows = new List<List<string>>();
                var start = 0;
                foreach (var end in split)
                {
                    rows.Add(parts[start..end].ToList());
                    start = end;
                }

                rows.Add(parts[start..].ToList());
                yield return rows;
            }
        }

        static List<int[]> BuildPartitionIndices(int length, int rowCount)
        {
            var results = new List<int[]>();
            var indices = new int[rowCount - 1];

            void Visit(int depth, int minNext)
            {
                if (depth == rowCount - 1)
                {
                    if (indices[^1] < length)
                    {
                        results.Add((int[])indices.Clone());
                    }

                    return;
                }

                var max = length - (rowCount - depth - 1);
                for (var i = minNext; i <= max; i++)
                {
                    indices[depth] = i;
                    Visit(depth + 1, i + 1);
                }
            }

            Visit(0, 1);
            return results;
        }

        static bool LayoutFits(
            IReadOnlyList<List<string>> rows,
            XGraphics g,
            XFont drawFont,
            double rowWidthLimit,
            double blockHeightLimit)
        {
            if (rows.Count == 0)
            {
                return false;
            }

            var bandHeight = blockHeightLimit / rows.Count;
            foreach (var row in rows)
            {
                if (RowHorizontalSpan(row, drawFont) > rowWidthLimit)
                {
                    return false;
                }

                if (RowVerticalExtent(g, row, drawFont) > bandHeight)
                {
                    return false;
                }
            }

            return true;
        }

        static List<List<string>>? FindBestRows(
            string[] parts,
            XGraphics g,
            XFont drawFont,
            double rowWidthLimit,
            double blockHeightLimit)
        {
            for (var rowCount = 1; rowCount <= parts.Length; rowCount++)
            {
                List<List<string>>? best = null;
                foreach (var candidate in ContiguousPartitions(parts, rowCount))
                {
                    if (!LayoutFits(candidate, g, drawFont, rowWidthLimit, blockHeightLimit))
                    {
                        continue;
                    }

                    if (best == null
                        || LayoutVerticalExtent(g, candidate, drawFont) < LayoutVerticalExtent(g, best, drawFont))
                    {
                        best = candidate;
                    }
                }

                if (best != null)
                {
                    return best;
                }
            }

            return null;
        }

        var activeFont = font;
        List<List<string>> rows;
        do
        {
            rows = FindBestRows(words, gfx, activeFont, maxRowWidth, maxBlockHeight)
                ?? PackRows(words, activeFont, maxRowWidth);
            if (LayoutFits(rows, gfx, activeFont, maxRowWidth, maxBlockHeight))
            {
                break;
            }

            if (activeFont.Size <= minFontSize)
            {
                break;
            }

            activeFont = new XFont(activeFont.Name, activeFont.Size - 0.25, activeFont.Style);
        }
        while (true);

        var pitch = WordPitch(activeFont);
        var rowExtents = rows
            .Select(row => RowVerticalExtent(gfx, row, activeFont))
            .ToArray();
        var rowStep = rows.Count > 1
            ? maxBlockHeight / rows.Count
            : maxBlockHeight;

        gfx.Save();
        gfx.IntersectClip(new XRect(cellX, cellY, cellWidth, cellHeight));
        gfx.TranslateTransform(cellX + (cellWidth / 2d), cellY + (cellHeight / 2d));
        gfx.RotateTransform(-90);

        var blockStartX = -(maxBlockHeight / 2d);
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            var rowExtent = rowExtents[rowIndex];
            var bandCenterX = blockStartX + (rowStep * rowIndex) + (rowStep / 2d);
            var rowSpan = RowHorizontalSpan(row, activeFont);
            var currentY = -(rowSpan / 2d);

            foreach (var word in row)
            {
                gfx.DrawString(
                    word,
                    activeFont,
                    XBrushes.Black,
                    new XRect(
                        bandCenterX - (rowExtent / 2d),
                        currentY,
                        rowExtent,
                        pitch),
                    XStringFormats.Center);

                currentY += pitch + segmentGap;
            }
        }

        gfx.Restore();
    }

    private static void DrawVertical(XGraphics gfx, string? text, XFont font, NdarPdfPoint point)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var lines = text
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return;
        }

        gfx.Save();
        gfx.TranslateTransform(point.X, point.Y);
        gfx.RotateTransform(-90);

        const double lineHeight = 7.2d;
        var totalHeight = lines.Length * lineHeight;
        var startY = Math.Max(0d, (34d - totalHeight) / 2d);
        for (var i = 0; i < lines.Length; i++)
        {
            gfx.DrawString(lines[i], font, XBrushes.Black, new XRect(0, startY + (i * lineHeight), 96, 9), XStringFormats.TopCenter);
        }

        gfx.Restore();
    }

    private static string FormatVerticalColumnLabel(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var words = name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return words.Length <= 1
            ? words[0]
            : string.Join('\n', words);
    }

    private static string FormatVerticalLabel(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var words = name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length <= 1)
        {
            return string.Join(' ', words);
        }

        const int maxCharsPerLine = 10;
        var lines = new List<string>();
        var currentLine = new List<string>();
        var currentLength = 0;
        foreach (var word in words)
        {
            var nextLength = currentLength == 0 ? word.Length : currentLength + 1 + word.Length;
            if (currentLine.Count > 0 && nextLength > maxCharsPerLine)
            {
                lines.Add(string.Join(' ', currentLine));
                currentLine.Clear();
                currentLength = 0;
            }

            currentLine.Add(word);
            currentLength = currentLength == 0 ? word.Length : currentLength + 1 + word.Length;
        }

        if (currentLine.Count > 0)
        {
            lines.Add(string.Join(' ', currentLine));
        }

        return string.Join("\n", lines);
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
            void DrawWrapped(string? text, TextBox2D box) =>
                DrawWrappedText(gfx, text, font, XBrushes.Black, box);
            void DrawMark(bool? value, bool yes, double x, double y, double boxSize = 9)
            {
                if (value.HasValue && value.Value == yes)
                {
                    DrawCheckboxX(gfx, x, y, boxSize);
                }
            }

            Draw(model.PermitNumber, map.PermitNumber.X, map.PermitNumber.Y, true);
            if (!string.IsNullOrWhiteSpace(model.GW59ADueDate?.ToString("MM/dd/yyyy")))
            {
                var dueDateRect = new XRect(
                    map.DueDateBox.X,
                    map.DueDateBox.Y,
                    map.DueDateBox.Width,
                    map.DueDateBox.Height);
                gfx.DrawString(
                    model.GW59ADueDate!.Value.ToString("MM/dd/yyyy"),
                    font,
                    XBrushes.Black,
                    dueDateRect,
                    XStringFormats.BottomCenter);
            }

            DrawMark(model.GW59AQuestion1Response, true, map.Q1Yes.X, map.Q1Yes.Y, map.Q1YesBoxSize);
            DrawMark(model.GW59AQuestion1Response, false, map.Q1No.X, map.Q1No.Y, map.Q1YesBoxSize);
            DrawMark(model.GW59AQuestion2Response, true, map.Q2Yes.X, map.Q2Yes.Y);
            DrawMark(model.GW59AQuestion2Response, false, map.Q2No.X, map.Q2No.Y);
            DrawMark(model.GW59AQuestion3Response, true, map.Q3Yes.X, map.Q3Yes.Y);
            DrawMark(model.GW59AQuestion3Response, false, map.Q3No.X, map.Q3No.Y);
            DrawMark(model.GW59AQuestion4Response, true, map.Q4Yes.X, map.Q4Yes.Y, map.Q4YesBoxSize);
            DrawMark(model.GW59AQuestion4Response, false, map.Q4No.X, map.Q4No.Y, map.Q4YesBoxSize);
            DrawMark(model.GW59AQuestion5Response, true, map.Q5Yes.X, map.Q5Yes.Y);
            DrawMark(model.GW59AQuestion5Response, false, map.Q5No.X, map.Q5No.Y);
            DrawMark(model.GW59AQuestion6Response, true, map.Q6Yes.X, map.Q6Yes.Y);
            DrawMark(model.GW59AQuestion6Response, false, map.Q6No.X, map.Q6No.Y);
            DrawMark(model.GW59AQuestion7Response, true, map.Q7Yes.X, map.Q7Yes.Y);
            DrawMark(model.GW59AQuestion7Response, false, map.Q7No.X, map.Q7No.Y);

            DrawWrapped(model.GW59AQuestion2Details, map.Q2Details);
            DrawWrapped(model.GW59AQuestion4Details, map.Q4Details);
            DrawWrapped(model.GW59AQuestion5Details, map.Q5Details);
            DrawWrapped(model.GW59AQuestion7Details, map.Q7Details);

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
        // GW-59A field positions on GW-59A.pdf (portrait letter).
        return new Gw59ACalibrationMap
        {
            PermitNumber = new Point2D(450, 18),
            // Due date blank is inside parentheses only: x=245.2–288.8 (center 267.0).
            DueDateBox = new TextBox2D(245.2, 57.6, 43.6, 11),
            // Q1 shares the first row with Q2; mark sits in the upper YES/NO sub-cell (70.6–91.2).
            Q1Yes = new Point2D(532.9, 76.4),
            Q1No = new Point2D(559.2, 76.4),
            Q1YesBoxSize = 8,
            Q2Yes = new Point2D(532.7, 98.0),
            Q2No = new Point2D(559.1, 98.0),
            Q3Yes = new Point2D(532.9, 187.8),
            Q3No = new Point2D(559.2, 187.8),
            // Q4 row band is 203–221; center the mark in the full row height.
            Q4Yes = new Point2D(532.7, 210.5),
            Q4No = new Point2D(559.1, 210.5),
            Q4YesBoxSize = 9,
            Q5Yes = new Point2D(532.7, 310.4),
            Q5No = new Point2D(559.1, 310.4),
            Q6Yes = new Point2D(532.9, 421.5),
            Q6No = new Point2D(559.2, 421.5),
            Q7Yes = new Point2D(532.9, 506.3),
            Q7No = new Point2D(559.2, 506.3),

            Q2Details = new TextBox2D(70, 142, 440, 36),
            Q4Details = new TextBox2D(70, 262, 440, 36),
            Q5Details = new TextBox2D(70, 364, 440, 66),
            Q7Details = new TextBox2D(70, 576, 440, 50),

            SignerName = new Point2D(80, 704),
            SignedDate = new Point2D(400, 704)
        };
    }

    private sealed class Gw59ACalibrationMap
    {
        public Point2D PermitNumber { get; set; }
        public TextBox2D DueDateBox { get; set; }
        public double Q1YesBoxSize { get; set; } = 9;
        public double Q4YesBoxSize { get; set; } = 9;
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
        public TextBox2D Q2Details { get; set; }
        public TextBox2D Q4Details { get; set; }
        public TextBox2D Q5Details { get; set; }
        public TextBox2D Q7Details { get; set; }
        public Point2D SignerName { get; set; }
        public Point2D SignedDate { get; set; }
    }

    private readonly record struct Point2D(double X, double Y);
    private readonly record struct TextBox2D(double X, double Y, double Width, double Height);

    private static void DrawWrappedText(XGraphics gfx, string? text, XFont font, XBrush brush, TextBox2D box)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var normalized = Regex.Replace(text.Trim(), @"\s+", " ");
        var lineHeight = gfx.MeasureString("Ag", font).Height;
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = new StringBuilder();
        var y = box.Y;

        foreach (var word in words)
        {
            var candidate = line.Length == 0 ? word : $"{line} {word}";
            var size = gfx.MeasureString(candidate, font);
            if (size.Width <= box.Width)
            {
                line.Clear();
                line.Append(candidate);
                continue;
            }

            if (line.Length > 0)
            {
                if (y + lineHeight > box.Y + box.Height)
                {
                    return;
                }

                gfx.DrawString(line.ToString(), font, brush, new XRect(box.X, y, box.Width, lineHeight), XStringFormats.TopLeft);
                y += lineHeight;
                line.Clear();
                line.Append(word);
            }
            else
            {
                if (y + lineHeight > box.Y + box.Height)
                {
                    return;
                }

                gfx.DrawString(word, font, brush, new XRect(box.X, y, box.Width, lineHeight), XStringFormats.TopLeft);
                y += lineHeight;
            }
        }

        if (line.Length > 0 && y + lineHeight <= box.Y + box.Height)
        {
            gfx.DrawString(line.ToString(), font, brush, new XRect(box.X, y, box.Width, lineHeight), XStringFormats.TopLeft);
        }
    }

    private static void DrawCheckboxX(XGraphics gfx, double boxLeft, double boxTop, double boxSize = 8)
    {
        var centerX = boxLeft + (boxSize / 2.0);
        var centerY = boxTop + (boxSize / 2.0);
        const double half = 2.5;
        gfx.DrawLine(XPens.Black, centerX - half, centerY - half, centerX + half, centerY + half);
        gfx.DrawLine(XPens.Black, centerX + half, centerY - half, centerX - half, centerY + half);
    }

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
