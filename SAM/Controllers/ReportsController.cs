using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    private readonly ISprayfieldService _sprayfieldService;

    public ReportsController(
        IIrrRprtService irrRprtService,
        IFacilityService facilityService,
        INDAR1Service ndar1Service,
        ISprayfieldService sprayfieldService,
        UserManager<ApplicationUser> userManager,
        ILogger<ReportsController> logger)
        : base(userManager, logger)
    {
        _irrRprtService = irrRprtService;
        _facilityService = facilityService;
        _ndar1Service = ndar1Service;
        _sprayfieldService = sprayfieldService;
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
        ViewBag.SelectedCompanyId = companyId;
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
    public async Task<IActionResult> NDAR1Reports(Guid? companyId = null, Guid? facilityId = null)
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

        var viewModel = new NDAR1EditViewModel
        {
            Id = report.Id,
            CompanyId = report.CompanyId,
            FacilityId = report.FacilityId,
            Month = report.Month,
            Year = report.Year,
            DidIrrigationOccur = report.DidIrrigationOccur,
            WeatherCodeDaily = report.WeatherCodeDaily,
            TemperatureDaily = report.TemperatureDaily,
            PrecipitationDaily = report.PrecipitationDaily,
            StorageDaily = report.StorageDaily,
            FiveDayUpsetDaily = report.FiveDayUpsetDaily,
            Field1Id = report.Field1Id,
            Field1VolumeAppliedDaily = report.Field1VolumeAppliedDaily,
            Field1TimeIrrigatedDaily = report.Field1TimeIrrigatedDaily,
            Field1DailyLoadingDaily = report.Field1DailyLoadingDaily,
            Field1MaxHourlyLoadingDaily = report.Field1MaxHourlyLoadingDaily,
            Field1MonthlyLoading = report.Field1MonthlyLoading,
            Field1MaxHourlyLoading = report.Field1MaxHourlyLoading,
            Field1TwelveMonthFloatingTotal = report.Field1TwelveMonthFloatingTotal,
            Field2Id = report.Field2Id,
            Field2VolumeAppliedDaily = report.Field2VolumeAppliedDaily,
            Field2TimeIrrigatedDaily = report.Field2TimeIrrigatedDaily,
            Field2DailyLoadingDaily = report.Field2DailyLoadingDaily,
            Field2MaxHourlyLoadingDaily = report.Field2MaxHourlyLoadingDaily,
            Field2MonthlyLoading = report.Field2MonthlyLoading,
            Field2MaxHourlyLoading = report.Field2MaxHourlyLoading,
            Field2TwelveMonthFloatingTotal = report.Field2TwelveMonthFloatingTotal,
            Field3Id = report.Field3Id,
            Field3VolumeAppliedDaily = report.Field3VolumeAppliedDaily,
            Field3TimeIrrigatedDaily = report.Field3TimeIrrigatedDaily,
            Field3DailyLoadingDaily = report.Field3DailyLoadingDaily,
            Field3MaxHourlyLoadingDaily = report.Field3MaxHourlyLoadingDaily,
            Field3MonthlyLoading = report.Field3MonthlyLoading,
            Field3MaxHourlyLoading = report.Field3MaxHourlyLoading,
            Field3TwelveMonthFloatingTotal = report.Field3TwelveMonthFloatingTotal,
            Field4Id = report.Field4Id,
            Field4VolumeAppliedDaily = report.Field4VolumeAppliedDaily,
            Field4TimeIrrigatedDaily = report.Field4TimeIrrigatedDaily,
            Field4DailyLoadingDaily = report.Field4DailyLoadingDaily,
            Field4MaxHourlyLoadingDaily = report.Field4MaxHourlyLoadingDaily,
            Field4MonthlyLoading = report.Field4MonthlyLoading,
            Field4MaxHourlyLoading = report.Field4MaxHourlyLoading,
            Field4TwelveMonthFloatingTotal = report.Field4TwelveMonthFloatingTotal
        };

        ViewBag.Facilities = await GetFacilitySelectListAsync(report.CompanyId);
        ViewBag.Months = GetMonthSelectList();
        ViewBag.Sprayfields = await GetSprayfieldSelectListAsync(report.FacilityId);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> NDAR1ReportEdit(NDAR1EditViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Months = GetMonthSelectList();
            ViewBag.Sprayfields = await GetSprayfieldSelectListAsync(viewModel.FacilityId);
            return View(viewModel);
        }

        try
        {
            var report = await _ndar1Service.GetByIdAsync(viewModel.Id);
            if (report == null)
                return NotFound();

            report.Month = viewModel.Month;
            report.Year = viewModel.Year;
            report.DidIrrigationOccur = viewModel.DidIrrigationOccur;
            report.WeatherCodeDaily = viewModel.WeatherCodeDaily;
            report.TemperatureDaily = viewModel.TemperatureDaily;
            report.PrecipitationDaily = viewModel.PrecipitationDaily;
            report.StorageDaily = viewModel.StorageDaily;
            report.FiveDayUpsetDaily = viewModel.FiveDayUpsetDaily;
            report.Field1Id = viewModel.Field1Id;
            report.Field1VolumeAppliedDaily = viewModel.Field1VolumeAppliedDaily;
            report.Field1TimeIrrigatedDaily = viewModel.Field1TimeIrrigatedDaily;
            report.Field1DailyLoadingDaily = viewModel.Field1DailyLoadingDaily;
            report.Field1MaxHourlyLoadingDaily = viewModel.Field1MaxHourlyLoadingDaily;
            report.Field1MonthlyLoading = viewModel.Field1MonthlyLoading;
            report.Field1MaxHourlyLoading = viewModel.Field1MaxHourlyLoading;
            report.Field1TwelveMonthFloatingTotal = viewModel.Field1TwelveMonthFloatingTotal;
            report.Field2Id = viewModel.Field2Id;
            report.Field2VolumeAppliedDaily = viewModel.Field2VolumeAppliedDaily;
            report.Field2TimeIrrigatedDaily = viewModel.Field2TimeIrrigatedDaily;
            report.Field2DailyLoadingDaily = viewModel.Field2DailyLoadingDaily;
            report.Field2MaxHourlyLoadingDaily = viewModel.Field2MaxHourlyLoadingDaily;
            report.Field2MonthlyLoading = viewModel.Field2MonthlyLoading;
            report.Field2MaxHourlyLoading = viewModel.Field2MaxHourlyLoading;
            report.Field2TwelveMonthFloatingTotal = viewModel.Field2TwelveMonthFloatingTotal;
            report.Field3Id = viewModel.Field3Id;
            report.Field3VolumeAppliedDaily = viewModel.Field3VolumeAppliedDaily;
            report.Field3TimeIrrigatedDaily = viewModel.Field3TimeIrrigatedDaily;
            report.Field3DailyLoadingDaily = viewModel.Field3DailyLoadingDaily;
            report.Field3MaxHourlyLoadingDaily = viewModel.Field3MaxHourlyLoadingDaily;
            report.Field3MonthlyLoading = viewModel.Field3MonthlyLoading;
            report.Field3MaxHourlyLoading = viewModel.Field3MaxHourlyLoading;
            report.Field3TwelveMonthFloatingTotal = viewModel.Field3TwelveMonthFloatingTotal;
            report.Field4Id = viewModel.Field4Id;
            report.Field4VolumeAppliedDaily = viewModel.Field4VolumeAppliedDaily;
            report.Field4TimeIrrigatedDaily = viewModel.Field4TimeIrrigatedDaily;
            report.Field4DailyLoadingDaily = viewModel.Field4DailyLoadingDaily;
            report.Field4MaxHourlyLoadingDaily = viewModel.Field4MaxHourlyLoadingDaily;
            report.Field4MonthlyLoading = viewModel.Field4MonthlyLoading;
            report.Field4MaxHourlyLoading = viewModel.Field4MaxHourlyLoading;
            report.Field4TwelveMonthFloatingTotal = viewModel.Field4TwelveMonthFloatingTotal;

            await _ndar1Service.UpdateAsync(report);
            TempData["SuccessMessage"] = "NDAR-1 report updated successfully.";
            return RedirectToAction(nameof(NDAR1ReportDetails), new { id = report.Id });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Months = GetMonthSelectList();
            ViewBag.Sprayfields = await GetSprayfieldSelectListAsync(viewModel.FacilityId);
            return View(viewModel);
        }
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
        var sprayfields = await _sprayfieldService.GetByFacilityIdAsync(facilityId);
        return new SelectList(sprayfields, "Id", "FieldId");
    }

    #endregion
}
