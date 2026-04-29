using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using System.Text.RegularExpressions;
using SAM.Controllers.Base;
using SAM.Domain.Entities;
using SAM.Infrastructure.Authorization;
using SAM.Services.Interfaces;
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

    public ReportsController(
        IIrrRprtService irrRprtService,
        IFacilityService facilityService,
        INDAR1Service ndar1Service,
        INDAR1RowEditService ndar1RowEditService,
        ISprayfieldService sprayfieldService,
        INDMRService ndmrService,
        INDMLRService ndmlrService,
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
            TempData["ErrorMessage"] = $"Error exporting NDMLR report: {ex.Message}";
            return RedirectToAction(nameof(NDAR1ReportDetails), new { id });
        }
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

    #endregion
}
