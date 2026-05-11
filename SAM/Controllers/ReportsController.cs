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

    #region Irrigation Reports

    [HttpGet]
    public async Task<IActionResult> IrrigationReports(Guid? companyId = null, Guid? facilityId = null)
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

        var reports = await _irrRprtService.GetAllAsync(companyId, facilityId);
        
        var viewModels = reports.Select(r => new IrrRprtViewModel
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
        });

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        ViewBag.Facilities = await GetFacilitySelectListAsync(companyId);
        ViewBag.SelectedFacilityId = facilityId;

        return View(viewModels);
    }

    [HttpGet]
    public async Task<IActionResult> IrrigationReportDetails(Guid id)
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

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> GenerateIrrigationReport(Guid? companyId = null, Guid? facilityId = null)
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

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> GenerateIrrigationReport(IrrRprtCreateViewModel viewModel)
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
            // Generate the report
            var report = await _irrRprtService.GenerateMonthlyReportAsync(
                viewModel.FacilityId,
                (int)viewModel.Month,
                viewModel.Year);

            // Save the generated report
            var savedReport = await _irrRprtService.CreateAsync(report);

            TempData["SuccessMessage"] = $"Monthly irrigation report generated successfully for {viewModel.Month} {viewModel.Year}.";
            return RedirectToAction(nameof(IrrigationReportDetails), new { id = savedReport.Id });
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
    public async Task<IActionResult> IrrigationReportEdit(Guid id)
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

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> IrrigationReportEdit(IrrRprtEditViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Months = GetMonthSelectList();
            ViewBag.ComplianceStatuses = GetComplianceStatusSelectList();
            return View(viewModel);
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
            TempData["SuccessMessage"] = $"Monthly irrigation report updated successfully.";
            return RedirectToAction(nameof(IrrigationReportDetails), new { id = report.Id });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Months = GetMonthSelectList();
            ViewBag.ComplianceStatuses = GetComplianceStatusSelectList();
            return View(viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> IrrigationReportDelete(Guid id)
    {
        IrrRprt? report = null;
        try
        {
            report = await _irrRprtService.GetByIdAsync(id);
            if (report == null)
                return NotFound();

            await EnsureCompanyAccessAsync(report.CompanyId);

            await _irrRprtService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Irrigation report deleted successfully.";
            return RedirectToAction(nameof(IrrigationReports), new {  facilityId = report.FacilityId });
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "Irrigation report not found.";
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(IrrigationReports), new { facilityId = report?.FacilityId });
    }

    #endregion

    #region NDAR-1 Reports

    [HttpGet]
    public async Task<IActionResult> NDAR1Reports( Guid? facilityId = null)
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

        var reports = await _ndar1Service.GetAllAsync(companyId, facilityId);
        
        var viewModels = reports.Select(r => new NDAR1ViewModel
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
            UpdatedDate = r.UpdatedDate
        });

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        ViewBag.Facilities = await GetFacilitySelectListAsync(companyId);
        ViewBag.SelectedCompanyId = companyId;
        ViewBag.SelectedFacilityId = facilityId;

        return View(viewModels);
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
                TempData["SuccessMessage"] = $"NDAR-1 report for {viewModel.Month} {viewModel.Year} already exists. Reusing the existing report.";
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
        int? year = null)
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

        var gwRecords = await _gwMonitService.GetAllAsync(companyId, facilityId, monitoringWellId);
        if (month.HasValue)
        {
            gwRecords = gwRecords.Where(x => x.SampleDate.Month == month.Value);
        }

        if (year.HasValue)
        {
            gwRecords = gwRecords.Where(x => x.SampleDate.Year == year.Value);
        }

        var rows = new List<GroundwaterQualityReportRowViewModel>();
        foreach (var record in gwRecords.OrderByDescending(x => x.SampleDate))
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
            SelectedFacilityId = facilityId,
            SelectedMonitoringWellId = monitoringWellId,
            SelectedMonth = month,
            SelectedYear = year,
            Rows = rows
        };

        var facilities = await _facilityService.GetAllAsync(companyId);
        ViewBag.Facilities = new SelectList(facilities, "Id", "Name", facilityId);
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
        ViewBag.MonitoringWells = new SelectList(wells, "Id", "WellId", monitoringWellId);
        ViewBag.Months = new SelectList(
            Enum.GetValues(typeof(MonthEnum)).Cast<MonthEnum>()
                .Select(e => new SelectListItem { Value = ((int)e).ToString(), Text = e.ToString() }),
            "Value",
            "Text",
            month?.ToString());
        ViewBag.Years = Enumerable.Range(DateTime.UtcNow.Year - 10, 21)
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
    public async Task<IActionResult> ExportGW59AReport(Guid id)
    {
        var model = await BuildGw59ExportModelAsync(id);
        var bytes = await RenderGw59APdfAsync(model);
        var safeFacility = string.IsNullOrWhiteSpace(model.FacilityName) ? "Facility" : model.FacilityName.Replace(' ', '_');
        return File(bytes, "application/pdf", $"GW59A_{safeFacility}_{model.SampleDate:yyyyMMdd}.pdf");
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
            ParameterSnapshots = snapshots
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

    private async Task<byte[]> RenderGw59APdfAsync(Gw59ExportModel model)
    {
        var templatePath = Path.Combine(_environment.WebRootPath, "forms", "GW-59A.pdf");
        if (!System.IO.File.Exists(templatePath))
        {
            throw new Infrastructure.Exceptions.BusinessRuleException("GW-59A template PDF not found in wwwroot/forms.");
        }

        using var output = new MemoryStream();
        using (var document = PdfReader.Open(templatePath, PdfDocumentOpenMode.Modify))
        {
            var page = document.Pages[0];
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 8, XFontStyle.Regular);
            var bold = new XFont("Arial", 8, XFontStyle.Bold);
            void Draw(string? text, double x, double y, bool isBold = false) =>
                gfx.DrawString(text ?? string.Empty, isBold ? bold : font, XBrushes.Black, new XRect(x, y, 320, 11), XStringFormats.TopLeft);

            Draw(model.FacilityName, 95, 58);
            Draw(model.PermitNumber, 620, 58);
            Draw(model.Permittee, 160, 75);
            Draw(model.Address, 105, 91);
            Draw($"{model.City}, {model.State} {model.ZipCode}", 105, 106);
            Draw(model.County, 365, 106);
            Draw(model.WellId, 170, 138);
            Draw(model.WellLocation, 380, 138);
            Draw(model.SampleDate.ToString("MM/dd/yyyy"), 655, 138);
            Draw(model.WaterLevel?.ToString("F2"), 170, 154);
            Draw(model.WellDepthFeet?.ToString("F2"), 380, 154);
            Draw(model.DiameterInches?.ToString("F2"), 548, 154);
            Draw(model.PHField?.ToString("F2"), 700, 154);
            Draw(model.TemperatureField?.ToString("F1"), 760, 154);
            Draw(model.SpecificConductance?.ToString("F2"), 640, 171);
            Draw(model.Odor, 640, 188);
            Draw(model.Appearance, 640, 204);
            Draw(model.LabName, 450, 221);
            Draw(model.LabCertificationNumber, 735, 221);
            Draw(model.CollectedBy, 108, 238);
            Draw(model.AnalyzedBy, 395, 238);
            Draw(model.CertificationName, 125, 720);
            Draw(model.CertificationTitle, 125, 736);
            Draw(model.CertificationDate?.ToString("MM/dd/yyyy"), 125, 752);

            var gw59aRows = model.ParameterSnapshots
                .Where(x => x.IsGw59A && !string.IsNullOrWhiteSpace(x.PcsCode))
                .OrderBy(x => x.PcsCode)
                .Take(28)
                .ToList();

            if (!gw59aRows.Any())
            {
                if (!model.HasGw59APermitTemplateRows)
                {
                    throw new Infrastructure.Exceptions.BusinessRuleException(
                        "No GW59A-tagged permit template rows were found for this record/permit period. " +
                        "Add Attachment C rows with report type 'Groundwater (GW59A)' under Permit Versions.");
                }

                throw new Infrastructure.Exceptions.BusinessRuleException(
                    "This groundwater record has no saved Attachment C template values to export. " +
                    "Open the groundwater monitoring record, confirm Template Parameters are loaded, then save the record again.");
            }

            var y = 274d;
            foreach (var row in gw59aRows)
            {
                Draw(row.PcsCode, 52, y);
                Draw(row.ParameterName, 98, y);
                Draw(row.Units, 390, y);
                Draw(row.Value?.ToString("0.##"), 520, y);
                Draw(row.DailyMaximumLimit?.ToString("0.##"), 660, y);
                y += 14;
            }

            document.Save(output, false);
        }

        await Task.CompletedTask;
        return output.ToArray();
    }

    #endregion
}
