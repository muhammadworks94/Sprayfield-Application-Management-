using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using SAM.Controllers.Base;
using SAM.Data;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using SAM.Domain.Entities;
using SAM.Domain.Extensions;
using SAM.Domain.Enums;
using SAM.Infrastructure.Authorization;
using SAM.Services.Interfaces;
using SAM.Services.Models;
using SAM.Services.Helpers;
using SAM.Utilities;
using SAM.ViewModels.OperationalData;

namespace SAM.Controllers;

/// <summary>
/// Controller for Operational Data Entry module - managing daily and monthly logs.
/// </summary>
[Authorize(Policy = Policies.RequireTechnicianOrOperator)]
    public class OperationalDataController : BaseController
    {
        private readonly IOperatorLogService _operatorLogService;
        private readonly IMonthlyApplicationService _monthlyApplicationService;
        private readonly IWWCharService _wwCharService;
        private readonly IGWMonitService _gwMonitService;
        private readonly IFacilityService _facilityService;
        private readonly ISprayfieldService _sprayfieldService;
        private readonly IApplicationComplianceService _applicationComplianceService;
        private readonly IMonitoringWellService _monitoringWellService;
        private readonly ILookupQueryService _lookupQueryService;
        private readonly IFacilityPermitResolver _facilityPermitResolver;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public OperationalDataController(
            IOperatorLogService operatorLogService,
            IMonthlyApplicationService monthlyApplicationService,
            IWWCharService wwCharService,
            IGWMonitService gwMonitService,
            IFacilityService facilityService,
            ISprayfieldService sprayfieldService,
            IApplicationComplianceService applicationComplianceService,
            IMonitoringWellService monitoringWellService,
            ILookupQueryService lookupQueryService,
            IFacilityPermitResolver facilityPermitResolver,
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            UserManager<ApplicationUser> userManager,
            ILogger<OperationalDataController> logger)
            : base(userManager, logger)
        {
            _operatorLogService = operatorLogService;
            _monthlyApplicationService = monthlyApplicationService;
            _wwCharService = wwCharService;
            _gwMonitService = gwMonitService;
            _facilityService = facilityService;
            _sprayfieldService = sprayfieldService;
            _applicationComplianceService = applicationComplianceService;
            _monitoringWellService = monitoringWellService;
            _lookupQueryService = lookupQueryService;
            _facilityPermitResolver = facilityPermitResolver;
            _context = context;
            _environment = environment;
        }

    #region Operator Logs

    [HttpGet]
    public async Task<IActionResult> OperatorLogs(Guid? companyId = null, Guid? facilityId = null)
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

        var logs = await _operatorLogService.GetAllAsync(companyId, facilityId);
        
        var viewModels = logs.Select(l => new OperatorLogViewModel
        {
            Id = l.Id,
            CompanyId = l.CompanyId,
            CompanyName = l.Company?.Name,
            FacilityId = l.FacilityId,
            FacilityName = l.Facility?.Name,
            LogDate = l.LogDate,
            OperatorName = l.OperatorName,
            WeatherConditions = l.WeatherConditions,
            TemperatureF = l.TemperatureF,
            PrecipitationIn = l.PrecipitationIn,
            StorageFt = l.StorageFt,
            FiveDayUpsetFt = l.FiveDayUpsetFt,
            ArrivalTime = l.ArrivalTime.ToString(@"hh\:mm"),
            TimeOnSiteHours = l.TimeOnSiteHours,
            MaintenancePerformed = l.MaintenancePerformed,
            EquipmentInspected = l.EquipmentInspected,
            IssuesNoted = l.IssuesNoted,
            CorrectiveActions = l.CorrectiveActions,
            NextShiftNotes = l.NextShiftNotes
        });

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        ViewBag.Facilities = await GetFacilitySelectListAsync(companyId);
        ViewBag.SelectedCompanyId = companyId;
        ViewBag.SelectedFacilityId = facilityId;

        return View(viewModels);
    }

    [HttpGet]
    public async Task<IActionResult> OperatorLogDetails(Guid id)
    {
        var log = await _operatorLogService.GetByIdAsync(id);
        if (log == null)
            return NotFound();

        await EnsureCompanyAccessAsync(log.CompanyId);

        var viewModel = new OperatorLogViewModel
        {
            Id = log.Id,
            CompanyId = log.CompanyId,
            CompanyName = log.Company?.Name,
            FacilityId = log.FacilityId,
            FacilityName = log.Facility?.Name,
            LogDate = log.LogDate,
            OperatorName = log.OperatorName,
            WeatherConditions = log.WeatherConditions,
            TemperatureF = log.TemperatureF,
            PrecipitationIn = log.PrecipitationIn,
            StorageFt = log.StorageFt,
            FiveDayUpsetFt = log.FiveDayUpsetFt,
            ArrivalTime = log.ArrivalTime.ToString(@"hh\:mm"),
            TimeOnSiteHours = log.TimeOnSiteHours,
            MaintenancePerformed = log.MaintenancePerformed,
            EquipmentInspected = log.EquipmentInspected,
            IssuesNoted = log.IssuesNoted,
            CorrectiveActions = log.CorrectiveActions,
            NextShiftNotes = log.NextShiftNotes
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> OperatorLogCreate(Guid? companyId = null, Guid? facilityId = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // Set company ID if not provided (respects session selection for admins)
        if (!companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        // If facility is selected but companyId is still unknown (e.g., global admin),
        // derive the company from the facility so downstream logic has a valid company.
        if (!companyId.HasValue && facilityId.HasValue)
        {
            var facility = await _facilityService.GetByIdAsync(facilityId.Value);
            if (facility != null)
            {
                companyId = facility.CompanyId;
            }
        }

        if (companyId.HasValue)
        {
            await EnsureCompanyAccessAsync(companyId.Value);
        }

        var viewModel = new OperatorLogCreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty,
            FacilityId = facilityId ?? Guid.Empty
        };

        ViewBag.Facilities = await GetFacilitySelectListAsync(companyId);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OperatorLogCreate(OperatorLogCreateViewModel viewModel)
    {
        // If company ID is empty but facility is selected, derive company from facility
        // This handles the case where admins don't have a company ID
        if (viewModel.CompanyId == Guid.Empty && viewModel.FacilityId != Guid.Empty)
        {
            var facility = await _facilityService.GetByIdAsync(viewModel.FacilityId);
            if (facility != null)
            {
                viewModel.CompanyId = facility.CompanyId;
            }
        }

        // Also check effective company ID from session for admins
        if (viewModel.CompanyId == Guid.Empty)
        {
            var effectiveCompanyId = await GetEffectiveCompanyIdAsync();
            if (effectiveCompanyId.HasValue)
            {
                viewModel.CompanyId = effectiveCompanyId.Value;
            }
        }

        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            return View(viewModel);
        }

        try
        {
            var currentUser = await GetCurrentUserAsync();
            var operatorName = currentUser == null
                ? null
                : (string.IsNullOrWhiteSpace(currentUser.FullName) ? currentUser.UserName : currentUser.FullName);

            var operatorLog = new OperatorLog
            {
                CompanyId = viewModel.CompanyId,
                FacilityId = viewModel.FacilityId,
                LogDate = viewModel.LogDate,
                OperatorName = operatorName ?? string.Empty,
                WeatherConditions = viewModel.WeatherConditions ?? string.Empty,
                TemperatureF = viewModel.TemperatureF,
                PrecipitationIn = viewModel.PrecipitationIn,
                StorageFt = viewModel.StorageFt,
                FiveDayUpsetFt = viewModel.FiveDayUpsetFt,
                ArrivalTime = TimeSpan.Parse(viewModel.ArrivalTime),
                TimeOnSiteHours = viewModel.TimeOnSiteHours ?? 0,
                MaintenancePerformed = viewModel.MaintenancePerformed ?? string.Empty,
                EquipmentInspected = viewModel.EquipmentInspected ?? string.Empty,
                IssuesNoted = viewModel.IssuesNoted ?? string.Empty,
                CorrectiveActions = viewModel.CorrectiveActions ?? string.Empty,
                NextShiftNotes = viewModel.NextShiftNotes ?? string.Empty
            };

            await _operatorLogService.CreateAsync(operatorLog);
            TempData["SuccessMessage"] = "Operator log created successfully.";
            return RedirectToAction(nameof(OperatorLogs));
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            return View(viewModel);
        }
    }

    [HttpGet]
    public async Task<IActionResult> OperatorLogEdit(Guid id)
    {
        var currentUser = await GetCurrentUserAsync();
        var operatorName = currentUser == null
            ? null
            : (string.IsNullOrWhiteSpace(currentUser.FullName) ? currentUser.UserName : currentUser.FullName);

        var log = await _operatorLogService.GetByIdAsync(id);
        if (log == null)
            return NotFound();

        await EnsureCompanyAccessAsync(log.CompanyId);

        var viewModel = new OperatorLogEditViewModel
        {
            Id = log.Id,
            CompanyId = log.CompanyId,
            FacilityId = log.FacilityId,
            LogDate = log.LogDate,
            OperatorName = operatorName == null ? log.OperatorName : operatorName,
            WeatherConditions = log.WeatherConditions,
            TemperatureF = log.TemperatureF,
            PrecipitationIn = log.PrecipitationIn,
            StorageFt = log.StorageFt,
            FiveDayUpsetFt = log.FiveDayUpsetFt,
            ArrivalTime = log.ArrivalTime.ToString(@"hh\:mm"),
            TimeOnSiteHours = log.TimeOnSiteHours,
            MaintenancePerformed = log.MaintenancePerformed,
            EquipmentInspected = log.EquipmentInspected,
            IssuesNoted = log.IssuesNoted,
            CorrectiveActions = log.CorrectiveActions,
            NextShiftNotes = log.NextShiftNotes
        };

        ViewBag.Facilities = await GetFacilitySelectListAsync(log.CompanyId);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OperatorLogEdit(OperatorLogEditViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            return View(viewModel);
        }

        try
        {
            var operatorLog = await _operatorLogService.GetByIdAsync(viewModel.Id);
            if (operatorLog == null)
                return NotFound();

            operatorLog.LogDate = viewModel.LogDate;
            operatorLog.WeatherConditions = viewModel.WeatherConditions ?? string.Empty;
            operatorLog.TemperatureF = viewModel.TemperatureF;
            operatorLog.PrecipitationIn = viewModel.PrecipitationIn;
            operatorLog.StorageFt = viewModel.StorageFt;
            operatorLog.FiveDayUpsetFt = viewModel.FiveDayUpsetFt;
            operatorLog.ArrivalTime = TimeSpan.Parse(viewModel.ArrivalTime);
            operatorLog.TimeOnSiteHours = viewModel.TimeOnSiteHours ?? 0;
            operatorLog.MaintenancePerformed = viewModel.MaintenancePerformed ?? string.Empty;
            operatorLog.EquipmentInspected = viewModel.EquipmentInspected ?? string.Empty;
            operatorLog.IssuesNoted = viewModel.IssuesNoted ?? string.Empty;
            operatorLog.CorrectiveActions = viewModel.CorrectiveActions ?? string.Empty;
            operatorLog.NextShiftNotes = viewModel.NextShiftNotes ?? string.Empty;

            await _operatorLogService.UpdateAsync(operatorLog);
            TempData["SuccessMessage"] = "Operator log updated successfully.";
            return RedirectToAction(nameof(OperatorLogs));
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            return View(viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OperatorLogDelete(Guid id)
    {
        try
        {
            var log = await _operatorLogService.GetByIdAsync(id);
            if (log != null)
            {
                await EnsureCompanyAccessAsync(log.CompanyId);
            }

            await _operatorLogService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Operator log deleted successfully.";
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "Operator log not found.";
        }

        return RedirectToAction(nameof(OperatorLogs));
    }

    #endregion

    #region Monthly Applications

    [HttpGet]
    public async Task<IActionResult> MonthlyApplications(Guid? facilityId = null, Guid? sprayfieldId = null)
    {
        var companyId = await GetEffectiveCompanyIdAsync();
        if (companyId.HasValue)
        {
            await EnsureCompanyAccessAsync(companyId.Value);
        }

        var applications = await _monthlyApplicationService.GetAllAsync(companyId, facilityId, sprayfieldId);
        var items = applications.Select(a => new MonthlyApplicationViewModel
        {
            Id = a.Id,
            CompanyId = a.CompanyId,
            FacilityId = a.FacilityId,
            FacilityName = a.Facility?.Name,
            SprayfieldId = a.SprayfieldId,
            ZoneName = a.Sprayfield?.FieldId,
            SprayfieldName = a.Sprayfield?.PermitFieldName ?? a.Sprayfield?.FieldId,
            ZoneAcres = a.Sprayfield is null ? null : SprayfieldReportHelper.GetReportAcres(a.Sprayfield),
            ApplicationDate = a.ApplicationDate,
            DailyLoadingInches = a.Sprayfield is null ? null : ComputeDailyLoadingFromVolume(a.VolumeGallons, SprayfieldReportHelper.GetReportAcres(a.Sprayfield)),
            VolumeGallons = a.VolumeGallons,
            TimeIrrigatedMinutes = a.TimeIrrigatedMinutes,
            MaximumHourlyLoadingInchesPerAcre = a.MaximumHourlyLoadingInchesPerAcre,
            OperatorSnapshotName = a.OperatorSnapshotName,
            Comments = a.Comments
        });

        ViewBag.Facilities = await GetFacilitySelectListAsync(companyId);
        ViewBag.Sprayfields = await GetSprayfieldSelectListAsync(companyId, facilityId);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> MonthlyApplicationCreate(Guid? facilityId = null, Guid? sprayfieldId = null)
    {
        var companyId = await GetEffectiveCompanyIdAsync();
        if (companyId.HasValue)
        {
            await EnsureCompanyAccessAsync(companyId.Value);
        }

        ViewBag.Facilities = await GetFacilitySelectListAsync(companyId);
        ViewBag.Sprayfields = await GetSprayfieldSelectListAsync(companyId, facilityId);

        return View(new MonthlyApplicationCreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty,
            FacilityId = facilityId ?? Guid.Empty,
            SprayfieldId = sprayfieldId ?? Guid.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MonthlyApplicationCreate(MonthlyApplicationCreateViewModel viewModel)
    {
        if (viewModel.SprayfieldId == Guid.Empty)
        {
            ModelState.AddModelError("SprayfieldId", "Sprayfield is required.");
        }

        var facility = await _facilityService.GetByIdAsync(viewModel.FacilityId);
        if (facility == null)
        {
            ModelState.AddModelError("FacilityId", "Selected facility was not found.");
        }
        else
        {
            viewModel.CompanyId = facility.CompanyId;
            await EnsureCompanyAccessAsync(facility.CompanyId);
        }

        Sprayfield? sprayfield = null;
        decimal computedVolumeGallons = 0m;

        if (ModelState.IsValid)
        {
            sprayfield = await _sprayfieldService.GetByIdAsync(viewModel.SprayfieldId);
            if (sprayfield == null || sprayfield.FacilityId != viewModel.FacilityId)
            {
                ModelState.AddModelError("SprayfieldId", "Selected sprayfield was not found for this facility.");
            }
            else
            {
                var acres = SprayfieldReportHelper.GetReportAcres(sprayfield);
                if (acres <= 0m)
                {
                    ModelState.AddModelError("SprayfieldId", "Sprayfield area (acres) must be configured before saving this monthly application.");
                }
                else
                {
                    if (!sprayfield.HourlyRateInches.HasValue)
                    {
                        ModelState.AddModelError("SprayfieldId", "Permitted (Max) Hourly Rate must be configured for this sprayfield before saving this monthly application.");
                    }
                    else
                    {
                        viewModel.MaximumHourlyLoadingInchesPerAcre = sprayfield.HourlyRateInches.Value;
                        viewModel.DailyLoadingInches = MonthlyApplicationCalculationHelper.ComputeDailyLoadingInches(
                            viewModel.TimeIrrigatedMinutes,
                            sprayfield.HourlyRateInches.Value);
                        computedVolumeGallons = viewModel.DailyLoadingInches.HasValue
                            ? MonthlyApplicationCalculationHelper.ComputeVolumeGallons(viewModel.DailyLoadingInches.Value, acres)
                            : 0m;
                        viewModel.VolumeGallons = computedVolumeGallons;
                    }
                }
            }
        }

        ComplianceProjectionResult? complianceProjection = null;
        if (ModelState.IsValid && facility != null)
        {
            try
            {
                complianceProjection = await _applicationComplianceService.GetProjectedComplianceAsync(new ComplianceProjectionRequest
                {
                    FacilityId = viewModel.FacilityId,
                    SprayfieldId = viewModel.SprayfieldId,
                    ApplicationDate = viewModel.ApplicationDate,
                    VolumeGallons = computedVolumeGallons
                });
            }
            catch (InvalidOperationException)
            {
                ModelState.AddModelError(string.Empty, BuildMissingWwCharMessage(viewModel.ApplicationDate));
                ViewBag.WWCharShortcutUrl = BuildWwCharCreateShortcutUrl(viewModel.FacilityId, viewModel.ApplicationDate);
            }

            if (complianceProjection?.RequiresConfirmation == true && !viewModel.ConfirmComplianceWarnings)
            {
                ModelState.AddModelError(string.Empty, string.Join(" ", complianceProjection.Warnings));
                viewModel.ComplianceWarningSummary = string.Join(" | ", complianceProjection.Warnings);
            }
        }

        if (!ModelState.IsValid)
        {
            var companyIdForLists = viewModel.CompanyId == Guid.Empty ? await GetEffectiveCompanyIdAsync() : viewModel.CompanyId;
            ViewBag.Facilities = await GetFacilitySelectListAsync(companyIdForLists);
            ViewBag.Sprayfields = await GetSprayfieldSelectListAsync(companyIdForLists, viewModel.FacilityId);
            ViewBag.WWCharShortcutUrl ??= BuildWwCharCreateShortcutUrl(viewModel.FacilityId, viewModel.ApplicationDate);
            return View(viewModel);
        }

        var currentUser = await GetCurrentUserAsync();
        var snapshotName = currentUser?.FullName ?? currentUser?.UserName ?? string.Empty;

        var application = new MonthlyApplication
        {
            CompanyId = facility!.CompanyId,
            FacilityId = viewModel.FacilityId,
            SprayfieldId = viewModel.SprayfieldId,
            ApplicationDate = viewModel.ApplicationDate,
            VolumeGallons = computedVolumeGallons,
            TimeIrrigatedMinutes = viewModel.TimeIrrigatedMinutes,
            MaximumHourlyLoadingInchesPerAcre = sprayfield!.HourlyRateInches!.Value,
            OperatorUserId = currentUser?.Id,
            OperatorSnapshotName = snapshotName,
            Comments = viewModel.Comments ?? string.Empty
        };

        await _monthlyApplicationService.CreateAsync(application);
        TempData["SuccessMessage"] = "Monthly application created successfully.";
        return RedirectToAction(nameof(MonthlyApplications), new { facilityId = viewModel.FacilityId });
    }

    [HttpGet]
    public async Task<IActionResult> MonthlyApplicationEdit(Guid id)
    {
        var application = await _monthlyApplicationService.GetByIdAsync(id);
        if (application == null)
            return NotFound();

        await EnsureCompanyAccessAsync(application.CompanyId);

        ViewBag.Facilities = await GetFacilitySelectListAsync(application.CompanyId);
        ViewBag.Sprayfields = await GetSprayfieldSelectListAsync(application.CompanyId, application.FacilityId);

        var areaAcres = application.Sprayfield is null ? 0m : SprayfieldReportHelper.GetReportAcres(application.Sprayfield);
        return View(new MonthlyApplicationEditViewModel
        {
            Id = application.Id,
            CompanyId = application.CompanyId,
            FacilityId = application.FacilityId,
            SprayfieldId = application.SprayfieldId,
            ApplicationDate = application.ApplicationDate,
            DailyLoadingInches = MonthlyApplicationCalculationHelper.ComputeDailyLoadingFromVolume(application.VolumeGallons, areaAcres),
            VolumeGallons = application.VolumeGallons,
            TimeIrrigatedMinutes = application.TimeIrrigatedMinutes,
            MaximumHourlyLoadingInchesPerAcre = application.Sprayfield?.HourlyRateInches ?? application.MaximumHourlyLoadingInchesPerAcre,
            Comments = application.Comments
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MonthlyApplicationEdit(MonthlyApplicationEditViewModel viewModel)
    {
        if (viewModel.SprayfieldId == Guid.Empty)
        {
            ModelState.AddModelError("SprayfieldId", "Sprayfield is required.");
        }

        var facility = await _facilityService.GetByIdAsync(viewModel.FacilityId);
        if (facility == null)
        {
            ModelState.AddModelError("FacilityId", "Selected facility was not found.");
        }
        else
        {
            viewModel.CompanyId = facility.CompanyId;
            await EnsureCompanyAccessAsync(facility.CompanyId);
        }

        Sprayfield? sprayfield = null;
        decimal computedVolumeGallons = 0m;

        if (ModelState.IsValid)
        {
            sprayfield = await _sprayfieldService.GetByIdAsync(viewModel.SprayfieldId);
            if (sprayfield == null || sprayfield.FacilityId != viewModel.FacilityId)
            {
                ModelState.AddModelError("SprayfieldId", "Selected sprayfield was not found for this facility.");
            }
            else
            {
                var acres = SprayfieldReportHelper.GetReportAcres(sprayfield);
                if (acres <= 0m)
                {
                    ModelState.AddModelError("SprayfieldId", "Sprayfield area (acres) must be configured before saving this monthly application.");
                }
                else if (!sprayfield.HourlyRateInches.HasValue)
                {
                    ModelState.AddModelError("SprayfieldId", "Permitted (Max) Hourly Rate must be configured for this sprayfield before saving this monthly application.");
                }
                else
                {
                    viewModel.MaximumHourlyLoadingInchesPerAcre = sprayfield.HourlyRateInches.Value;
                    viewModel.DailyLoadingInches = MonthlyApplicationCalculationHelper.ComputeDailyLoadingInches(
                        viewModel.TimeIrrigatedMinutes,
                        sprayfield.HourlyRateInches.Value);
                    computedVolumeGallons = viewModel.DailyLoadingInches.HasValue
                        ? MonthlyApplicationCalculationHelper.ComputeVolumeGallons(viewModel.DailyLoadingInches.Value, acres)
                        : 0m;
                    viewModel.VolumeGallons = computedVolumeGallons;
                }
            }
        }

        ComplianceProjectionResult? complianceProjection = null;
        if (ModelState.IsValid && facility != null)
        {
            try
            {
                complianceProjection = await _applicationComplianceService.GetProjectedComplianceAsync(new ComplianceProjectionRequest
                {
                    FacilityId = viewModel.FacilityId,
                    SprayfieldId = viewModel.SprayfieldId,
                    ApplicationDate = viewModel.ApplicationDate,
                    VolumeGallons = computedVolumeGallons,
                    ExistingApplicationId = viewModel.Id
                });
            }
            catch (InvalidOperationException)
            {
                ModelState.AddModelError(string.Empty, BuildMissingWwCharMessage(viewModel.ApplicationDate));
                ViewBag.WWCharShortcutUrl = BuildWwCharCreateShortcutUrl(viewModel.FacilityId, viewModel.ApplicationDate);
            }

            if (complianceProjection?.RequiresConfirmation == true && !viewModel.ConfirmComplianceWarnings)
            {
                ModelState.AddModelError(string.Empty, string.Join(" ", complianceProjection.Warnings));
                viewModel.ComplianceWarningSummary = string.Join(" | ", complianceProjection.Warnings);
            }
        }

        if (!ModelState.IsValid)
        {
            var companyIdForLists = viewModel.CompanyId == Guid.Empty ? await GetEffectiveCompanyIdAsync() : viewModel.CompanyId;
            ViewBag.Facilities = await GetFacilitySelectListAsync(companyIdForLists);
            ViewBag.Sprayfields = await GetSprayfieldSelectListAsync(companyIdForLists, viewModel.FacilityId);
            ViewBag.WWCharShortcutUrl ??= BuildWwCharCreateShortcutUrl(viewModel.FacilityId, viewModel.ApplicationDate);
            return View(viewModel);
        }

        var application = await _monthlyApplicationService.GetByIdAsync(viewModel.Id);
        if (application == null)
            return NotFound();

        application.CompanyId = facility!.CompanyId;
        application.FacilityId = viewModel.FacilityId;
        application.SprayfieldId = viewModel.SprayfieldId;
        application.ApplicationDate = viewModel.ApplicationDate;
        application.VolumeGallons = computedVolumeGallons;
        application.TimeIrrigatedMinutes = viewModel.TimeIrrigatedMinutes;
        application.MaximumHourlyLoadingInchesPerAcre = sprayfield!.HourlyRateInches!.Value;
        application.Comments = viewModel.Comments ?? string.Empty;

        await _monthlyApplicationService.UpdateAsync(application);
        TempData["SuccessMessage"] = "Monthly application updated successfully.";
        return RedirectToAction(nameof(MonthlyApplications), new { facilityId = viewModel.FacilityId });
    }

    [HttpGet]
    public async Task<IActionResult> MonthlyApplicationDetails(Guid id)
    {
        var application = await _monthlyApplicationService.GetByIdAsync(id);
        if (application == null)
            return NotFound();

        await EnsureCompanyAccessAsync(application.CompanyId);

        var viewModel = new MonthlyApplicationViewModel
        {
            Id = application.Id,
            CompanyId = application.CompanyId,
            CompanyName = application.Company?.Name,
            FacilityId = application.FacilityId,
            FacilityName = application.Facility?.Name,
            SprayfieldId = application.SprayfieldId,
            ZoneName = application.Sprayfield?.FieldId,
            SprayfieldName = application.Sprayfield?.PermitFieldName ?? application.Sprayfield?.FieldId,
            ZoneAcres = application.Sprayfield is null ? null : SprayfieldReportHelper.GetReportAcres(application.Sprayfield),
            ApplicationDate = application.ApplicationDate,
            DailyLoadingInches = application.Sprayfield is null ? null : ComputeDailyLoadingFromVolume(application.VolumeGallons, SprayfieldReportHelper.GetReportAcres(application.Sprayfield)),
            VolumeGallons = application.VolumeGallons,
            TimeIrrigatedMinutes = application.TimeIrrigatedMinutes,
            MaximumHourlyLoadingInchesPerAcre = application.MaximumHourlyLoadingInchesPerAcre,
            OperatorSnapshotName = application.OperatorSnapshotName,
            Comments = application.Comments
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MonthlyApplicationDelete(Guid id)
    {
        try
        {
            var application = await _monthlyApplicationService.GetByIdAsync(id);
            if (application == null)
            {
                TempData["ErrorMessage"] = "Monthly application not found.";
                return RedirectToAction(nameof(MonthlyApplications));
            }

            await EnsureCompanyAccessAsync(application.CompanyId);
            var facilityId = application.FacilityId;

            await _monthlyApplicationService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Monthly application deleted successfully.";
            return RedirectToAction(nameof(MonthlyApplications), new { facilityId });
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "Monthly application not found.";
            return RedirectToAction(nameof(MonthlyApplications));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetSprayfieldsForFacility(Guid facilityId)
    {
        if (facilityId == Guid.Empty)
            return BadRequest("Facility is required.");

        var facility = await _facilityService.GetByIdAsync(facilityId);
        if (facility == null)
            return NotFound("Facility not found.");

        await EnsureCompanyAccessAsync(facility.CompanyId);

        var sprayfields = await _sprayfieldService.GetAllAsync(facility.CompanyId);
        var filtered = sprayfields
            .Where(s => s.FacilityId == facilityId)
            .Select(s => new
            {
                id = s.Id,
                name = s.PermitFieldName ?? s.FieldId,
                acres = SprayfieldReportHelper.GetReportAcres(s),
                hourlyRateInches = s.HourlyRateInches
            })
            .ToList();

        return Json(new { companyId = facility.CompanyId, sprayfields = filtered });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidateMonthlyApplication([FromBody] ComplianceProjectionRequest request)
    {
        if (request.FacilityId == Guid.Empty)
        {
            return BadRequest(new { message = "Facility is required." });
        }

        if (request.SprayfieldId == Guid.Empty)
        {
            return BadRequest(new { message = "Sprayfield is required." });
        }

        var facility = await _facilityService.GetByIdAsync(request.FacilityId);
        if (facility == null)
        {
            return NotFound(new { message = "Facility not found." });
        }

        await EnsureCompanyAccessAsync(facility.CompanyId);

        var sprayfield = await _sprayfieldService.GetByIdAsync(request.SprayfieldId);
        if (sprayfield == null || sprayfield.FacilityId != request.FacilityId)
        {
            return BadRequest(new { message = "Selected sprayfield was not found for this facility." });
        }

        var acres = SprayfieldReportHelper.GetReportAcres(sprayfield);
        if (acres <= 0m)
        {
            return BadRequest(new { message = "Sprayfield area (acres) must be configured before saving this monthly application." });
        }

        if (!sprayfield.HourlyRateInches.HasValue)
        {
            return BadRequest(new { message = "Permitted (Max) Hourly Rate must be configured for this sprayfield before saving this monthly application." });
        }

        ComplianceProjectionResult projection;
        try
        {
            projection = await _applicationComplianceService.GetProjectedComplianceAsync(request);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = BuildMissingWwCharMessage(request.ApplicationDate),
                wwCharShortcutUrl = BuildWwCharCreateShortcutUrl(request.FacilityId, request.ApplicationDate),
                detail = ex.Message
            });
        }

        return Json(new
        {
            projection.WindowStartDate,
            projection.WindowEndDate,
            projection.SprayfieldId,
            projection.SprayfieldName,
            projection.FieldAcres,
            projection.ProjectedPanLbsPerAcre,
            projection.PanLimitLbsPerAcre,
            projection.PanUtilizationPercent,
            projection.ProjectedHydraulicInches,
            projection.HydraulicLimitInchesPerYear,
            projection.HydraulicUtilizationPercent,
            projection.RequiresConfirmation,
            projection.Warnings
        });
    }

    #endregion

    #region Wastewater Characteristics (WWChar)

    [HttpGet]
    public async Task<IActionResult> WWChars(Guid? facilityId = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // Resolve company from global selection (header) only; no companyId is passed via query anymore
        Guid? companyId = effectiveCompanyId;

        if (companyId.HasValue)
        {
            await EnsureCompanyAccessAsync(companyId.Value);
        }

        var wwChars = await _wwCharService.GetAllAsync(companyId, facilityId);
        
        var viewModels = wwChars.Select(w => new WWCharViewModel
        {
            Id = w.Id,
            CompanyId = w.CompanyId,
            CompanyName = w.Company?.Name,
            FacilityId = w.FacilityId,
            FacilityName = w.Facility?.Name,
            Month = w.Month,
            Year = w.Year,
            BOD5Daily = w.BOD5Daily,
            TSSDaily = w.TSSDaily,
            FlowRateDaily = w.FlowRateDaily,
            PHDaily = w.PHDaily,
            NH3NDaily = w.NH3NDaily,
            FecalColiformDaily = w.FecalColiformDaily,
            ChlorideDaily = w.ChlorideDaily,
            CaDaily = w.CaDaily,
            MgDaily = w.MgDaily,
            NaDaily = w.NaDaily,
            SARDaily = w.SARDaily,
            TNDaily = w.TNDaily,
            CompositeTime = w.CompositeTime,
            ORCOnSite = w.ORCOnSite,
            LagoonFreeboard = w.LagoonFreeboard,
            LabCertification = w.LabCertification,
            CollectedBy = w.CollectedBy,
            AnalyzedBy = w.AnalyzedBy,
            FacilityPermitId = w.FacilityPermitId,
            FacilityPermitDisplay = w.FacilityPermit != null ? $"{w.FacilityPermit.PermitNumber} v{w.FacilityPermit.PermitVersion}" : null,
            FlowMeasuringPoint = w.FlowMeasuringPoint,
            ParameterMonitoringPoint = w.ParameterMonitoringPoint
        });

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        ViewBag.Facilities = await GetFacilitySelectListAsync(companyId);
        ViewBag.SelectedCompanyId = companyId;
        ViewBag.SelectedFacilityId = facilityId;

        return View(viewModels);
    }

    [HttpGet]
    public async Task<IActionResult> WWCharDetails(Guid id)
    {
        var wwChar = await _wwCharService.GetByIdAsync(id);
        if (wwChar == null)
            return NotFound();

        await EnsureCompanyAccessAsync(wwChar.CompanyId);

        var viewModel = new WWCharViewModel
        {
            Id = wwChar.Id,
            CompanyId = wwChar.CompanyId,
            CompanyName = wwChar.Company?.Name,
            FacilityId = wwChar.FacilityId,
            FacilityName = wwChar.Facility?.Name,
            Month = wwChar.Month,
            Year = wwChar.Year,
            BOD5Daily = wwChar.BOD5Daily,
            TSSDaily = wwChar.TSSDaily,
            FlowRateDaily = wwChar.FlowRateDaily,
            PHDaily = wwChar.PHDaily,
            NH3NDaily = wwChar.NH3NDaily,
            FecalColiformDaily = wwChar.FecalColiformDaily,
            ChlorideDaily = wwChar.ChlorideDaily,
            CaDaily = wwChar.CaDaily,
            MgDaily = wwChar.MgDaily,
            NaDaily = wwChar.NaDaily,
            SARDaily = wwChar.SARDaily,
            TNDaily = wwChar.TNDaily,
            CompositeTime = wwChar.CompositeTime,
            ORCOnSite = wwChar.ORCOnSite,
            LagoonFreeboard = wwChar.LagoonFreeboard,
            LabCertification = wwChar.LabCertification,
            CollectedBy = wwChar.CollectedBy,
            AnalyzedBy = wwChar.AnalyzedBy,
            FacilityPermitId = wwChar.FacilityPermitId,
            FacilityPermitDisplay = wwChar.FacilityPermit != null ? $"{wwChar.FacilityPermit.PermitNumber} v{wwChar.FacilityPermit.PermitVersion}" : null,
            NO2N = wwChar.NO2N,
            TKNN = wwChar.TKNN,
            NO3N = wwChar.NO3N,
            FlowMeasuringPoint = wwChar.FlowMeasuringPoint,
            ParameterMonitoringPoint = wwChar.ParameterMonitoringPoint
        };

        viewModel.TemplateParameters = await BuildWwCharTemplateInputsAsync(
            wwChar.CompanyId,
            wwChar.FacilityId,
            wwChar.FacilityPermitId,
            new DateTime(wwChar.Year, (int)wwChar.Month, 1),
            wwChar.Id);
        viewModel.TemplateParametersStatusMessage = await BuildWwCharTemplateStatusMessageAsync(
            wwChar.CompanyId,
            wwChar.FacilityId,
            wwChar.FacilityPermitId,
            new DateTime(wwChar.Year, (int)wwChar.Month, 1),
            viewModel.TemplateParameters.Count);

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireTechnician)]
    public async Task<IActionResult> WWCharCreate(Guid? companyId = null, Guid? facilityId = null, int? month = null, int? year = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // Set company ID if not provided (respects session selection for admins)
        if (!companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        // If facility is selected but companyId is still unknown (e.g., global admin),
        // derive the company from the facility so downstream logic has a valid company.
        if (!companyId.HasValue && facilityId.HasValue)
        {
            var facility = await _facilityService.GetByIdAsync(facilityId.Value);
            if (facility != null)
            {
                companyId = facility.CompanyId;
            }
        }

        if (companyId.HasValue)
        {
            await EnsureCompanyAccessAsync(companyId.Value);
        }

        var viewModel = new WWCharCreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty,
            FacilityId = facilityId ?? Guid.Empty
        };

        if (month.HasValue && month.Value >= 1 && month.Value <= 12)
        {
            viewModel.Month = (MonthEnum)month.Value;
        }

        if (year.HasValue && year.Value >= 2000 && year.Value <= 2100)
        {
            viewModel.Year = year.Value;
        }

        // Initialize daily arrays with 31 empty entries
        InitializeDailyArrays(viewModel);

        if (viewModel.FacilityId != Guid.Empty)
        {
            var reportDate = new DateTime(viewModel.Year, (int)viewModel.Month, 1);
            var resolvedPermit = await _facilityPermitResolver.ResolveForDateAsync(viewModel.FacilityId, reportDate);
            viewModel.FacilityPermitId = resolvedPermit?.Id;
            viewModel.FacilityPermitDisplay = resolvedPermit != null ? $"{resolvedPermit.PermitNumber} v{resolvedPermit.PermitVersion}" : null;
            viewModel.TemplateParameters = await BuildWwCharTemplateInputsAsync(
                viewModel.CompanyId,
                viewModel.FacilityId,
                resolvedPermit?.Id,
                reportDate,
                null);
            viewModel.TemplateParametersStatusMessage = await BuildWwCharTemplateStatusMessageAsync(
                viewModel.CompanyId,
                viewModel.FacilityId,
                resolvedPermit?.Id,
                reportDate,
                viewModel.TemplateParameters.Count);
        }

        ViewBag.Facilities = await GetFacilitySelectListAsync(companyId);
        ViewBag.Months = GetMonthSelectList();
        ViewBag.ORCOnSiteOptions = GetORCOnSiteSelectList();
        ViewBag.FlowMeasuringPointOptions = GetFlowMeasuringPointSelectList();
        ViewBag.ParameterMonitoringPointOptions = GetParameterMonitoringPointSelectList();

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireTechnician)]
    public async Task<IActionResult> WWCharCreate(WWCharCreateViewModel viewModel)
    {
        // If CompanyId is not set (e.g., global admin without a company) but FacilityId is,
        // resolve the company from the selected facility.
        if ((viewModel.CompanyId == Guid.Empty || viewModel.CompanyId == default) && viewModel.FacilityId != Guid.Empty)
        {
            var facility = await _facilityService.GetByIdAsync(viewModel.FacilityId);
            if (facility != null)
            {
                viewModel.CompanyId = facility.CompanyId;
            }
        }

        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        // Ensure arrays are initialized
        EnsureDailyArraysInitialized(viewModel);
        var createReportDate = new DateTime(viewModel.Year, (int)viewModel.Month, 1);
        var resolvedCreatePermit = viewModel.FacilityId != Guid.Empty
            ? await _facilityPermitResolver.ResolveForDateAsync(viewModel.FacilityId, createReportDate)
            : null;
        viewModel.FacilityPermitId = resolvedCreatePermit?.Id;
        viewModel.FacilityPermitDisplay = resolvedCreatePermit != null ? $"{resolvedCreatePermit.PermitNumber} v{resolvedCreatePermit.PermitVersion}" : null;
        if (!viewModel.TemplateParameters.Any())
        {
            viewModel.TemplateParameters = await BuildWwCharTemplateInputsAsync(viewModel.CompanyId, viewModel.FacilityId, resolvedCreatePermit?.Id, createReportDate, null);
        }
        viewModel.TemplateParametersStatusMessage = await BuildWwCharTemplateStatusMessageAsync(
            viewModel.CompanyId,
            viewModel.FacilityId,
            resolvedCreatePermit?.Id,
            createReportDate,
            viewModel.TemplateParameters.Count);
        EnsureTemplateArraysInitialized(viewModel.TemplateParameters);

        if (!ModelState.IsValid)
        {
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Months = GetMonthSelectList();
            ViewBag.ORCOnSiteOptions = GetORCOnSiteSelectList();
            ViewBag.FlowMeasuringPointOptions = GetFlowMeasuringPointSelectList();
            ViewBag.ParameterMonitoringPointOptions = GetParameterMonitoringPointSelectList();
            return View(viewModel);
        }

        try
        {
            var wwChar = new WWChar
            {
                CompanyId = viewModel.CompanyId,
                FacilityId = viewModel.FacilityId,
                Month = viewModel.Month,
                Year = viewModel.Year,
                BOD5Daily = viewModel.BOD5Daily,
                TSSDaily = viewModel.TSSDaily,
                FlowRateDaily = viewModel.FlowRateDaily,
                PHDaily = viewModel.PHDaily,
                NH3NDaily = viewModel.NH3NDaily,
                FecalColiformDaily = viewModel.FecalColiformDaily,
                ChlorideDaily = viewModel.ChlorideDaily,
                CaDaily = viewModel.CaDaily,
                MgDaily = viewModel.MgDaily,
                NaDaily = viewModel.NaDaily,
                SARDaily = viewModel.SARDaily,
                TNDaily = viewModel.TNDaily,
                CompositeTime = viewModel.CompositeTime,
                ORCOnSite = viewModel.ORCOnSite,
                LagoonFreeboard = viewModel.LagoonFreeboard,
                LabCertification = viewModel.LabCertification,
                CollectedBy = viewModel.CollectedBy,
                AnalyzedBy = viewModel.AnalyzedBy,
                FacilityPermitId = viewModel.FacilityPermitId,
                FlowMeasuringPoint = viewModel.FlowMeasuringPoint,
                ParameterMonitoringPoint = viewModel.ParameterMonitoringPoint
            };
            ApplyLegacyChemistrySnapshotsFromTemplate(wwChar, viewModel.TemplateParameters);

            var savedWwChar = await _wwCharService.CreateAsync(wwChar);
            await SaveWwCharTemplateValuesAsync(savedWwChar, viewModel.TemplateParameters);
            TempData["SuccessMessage"] = $"Wastewater characteristics record saved for {savedWwChar.Month} {savedWwChar.Year}.";
            return RedirectToAction(nameof(WWChars), new { facilityId = savedWwChar.FacilityId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Months = GetMonthSelectList();
            ViewBag.ORCOnSiteOptions = GetORCOnSiteSelectList();
            ViewBag.FlowMeasuringPointOptions = GetFlowMeasuringPointSelectList();
            ViewBag.ParameterMonitoringPointOptions = GetParameterMonitoringPointSelectList();
            if (!viewModel.TemplateParameters.Any())
            {
                viewModel.TemplateParameters = await BuildWwCharTemplateInputsAsync(viewModel.CompanyId, viewModel.FacilityId, resolvedCreatePermit?.Id, createReportDate, null);
            }
            viewModel.TemplateParametersStatusMessage = await BuildWwCharTemplateStatusMessageAsync(
                viewModel.CompanyId,
                viewModel.FacilityId,
                resolvedCreatePermit?.Id,
                createReportDate,
                viewModel.TemplateParameters.Count);
            return View(viewModel);
        }
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireTechnician)]
    public async Task<IActionResult> WWCharEdit(Guid id)
    {
        var wwChar = await _wwCharService.GetByIdAsync(id);
        if (wwChar == null)
            return NotFound();

        await EnsureCompanyAccessAsync(wwChar.CompanyId);

        var viewModel = new WWCharEditViewModel
        {
            Id = wwChar.Id,
            CompanyId = wwChar.CompanyId,
            FacilityId = wwChar.FacilityId,
            Month = wwChar.Month,
            Year = wwChar.Year,
            BOD5Daily = wwChar.BOD5Daily,
            TSSDaily = wwChar.TSSDaily,
            FlowRateDaily = wwChar.FlowRateDaily,
            PHDaily = wwChar.PHDaily,
            NH3NDaily = wwChar.NH3NDaily,
            FecalColiformDaily = wwChar.FecalColiformDaily,
            ChlorideDaily = wwChar.ChlorideDaily,
            CaDaily = wwChar.CaDaily,
            MgDaily = wwChar.MgDaily,
            NaDaily = wwChar.NaDaily,
            SARDaily = wwChar.SARDaily,
            TNDaily = wwChar.TNDaily,
            CompositeTime = wwChar.CompositeTime,
            ORCOnSite = wwChar.ORCOnSite,
            LagoonFreeboard = wwChar.LagoonFreeboard,
            LabCertification = wwChar.LabCertification,
            CollectedBy = wwChar.CollectedBy,
            AnalyzedBy = wwChar.AnalyzedBy,
            NO2N = wwChar.NO2N.HasValue ? Math.Round(wwChar.NO2N.Value, 2) : (decimal?)null,
            TKNN = wwChar.TKNN.HasValue ? Math.Round(wwChar.TKNN.Value, 2) : (decimal?)null,
            NO3N = wwChar.NO3N.HasValue ? Math.Round(wwChar.NO3N.Value, 2) : (decimal?)null,
            FacilityPermitId = wwChar.FacilityPermitId,
            FacilityPermitDisplay = wwChar.FacilityPermit != null ? $"{wwChar.FacilityPermit.PermitNumber} v{wwChar.FacilityPermit.PermitVersion}" : null,
            FlowMeasuringPoint = wwChar.FlowMeasuringPoint,
            ParameterMonitoringPoint = wwChar.ParameterMonitoringPoint
        };

        // Ensure arrays are initialized with 31 entries
        EnsureDailyArraysInitialized(viewModel);
        viewModel.TemplateParameters = await BuildWwCharTemplateInputsAsync(
            wwChar.CompanyId,
            wwChar.FacilityId,
            wwChar.FacilityPermitId,
            new DateTime(wwChar.Year, (int)wwChar.Month, 1),
            wwChar.Id);
        viewModel.TemplateParametersStatusMessage = await BuildWwCharTemplateStatusMessageAsync(
            wwChar.CompanyId,
            wwChar.FacilityId,
            wwChar.FacilityPermitId,
            new DateTime(wwChar.Year, (int)wwChar.Month, 1),
            viewModel.TemplateParameters.Count);

        ViewBag.Facilities = await GetFacilitySelectListAsync(wwChar.CompanyId);
        ViewBag.Months = GetMonthSelectList();
        ViewBag.ORCOnSiteOptions = GetORCOnSiteSelectList();
        ViewBag.FlowMeasuringPointOptions = GetFlowMeasuringPointSelectList();
        ViewBag.ParameterMonitoringPointOptions = GetParameterMonitoringPointSelectList();

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireTechnician)]
    public async Task<IActionResult> WWCharTemplateContext(Guid facilityId, int month, int year, Guid? recordId = null, bool isEdit = false)
    {
        if (facilityId == Guid.Empty || month < 1 || month > 12 || year < 2000 || year > 2100)
        {
            return BadRequest("Invalid template context parameters.");
        }

        var facility = await _facilityService.GetByIdAsync(facilityId);
        if (facility == null)
        {
            return NotFound();
        }

        await EnsureCompanyAccessAsync(facility.CompanyId);

        var reportDate = new DateTime(year, month, 1);
        var resolvedPermit = await _facilityPermitResolver.ResolveForDateAsync(facilityId, reportDate);
        var templateParameters = await BuildWwCharTemplateInputsAsync(
            facility.CompanyId,
            facilityId,
            resolvedPermit?.Id,
            reportDate,
            recordId);
        var statusMessage = await BuildWwCharTemplateStatusMessageAsync(
            facility.CompanyId,
            facilityId,
            resolvedPermit?.Id,
            reportDate,
            templateParameters.Count);

        List<ORCOnSiteEnum?> orcOnSite = new();
        List<decimal?> lagoonFreeboard = new();
        if (recordId.HasValue)
        {
            var existingRecord = await _wwCharService.GetByIdAsync(recordId.Value);
            if (existingRecord != null)
            {
                await EnsureCompanyAccessAsync(existingRecord.CompanyId);
                orcOnSite = existingRecord.ORCOnSite;
                lagoonFreeboard = existingRecord.LagoonFreeboard;
            }
        }

        var vm = new WWCharTemplateSectionViewModel
        {
            FacilityId = facilityId,
            FacilityPermitId = resolvedPermit?.Id,
            FacilityPermitDisplay = resolvedPermit != null ? $"{resolvedPermit.PermitNumber} v{resolvedPermit.PermitVersion}" : null,
            Month = (MonthEnum)month,
            Year = year,
            RecordId = recordId,
            IsEdit = isEdit,
            TemplateParametersStatusMessage = statusMessage,
            TemplateParameters = templateParameters,
            ORCOnSite = orcOnSite,
            LagoonFreeboard = lagoonFreeboard
        };
        EnsureTemplateArraysInitialized(vm.TemplateParameters);
        EnsureDayArraysInitialized(vm);

        ViewBag.ORCOnSiteOptions = GetORCOnSiteSelectList();
        return PartialView("Partials/_WWCharTemplateSection", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireTechnician)]
    public async Task<IActionResult> WWCharEdit(WWCharEditViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        // Ensure arrays are initialized
        EnsureDailyArraysInitialized(viewModel);
        var editReportDate = new DateTime(viewModel.Year, (int)viewModel.Month, 1);
        var resolvedEditPermit = viewModel.FacilityId != Guid.Empty
            ? await _facilityPermitResolver.ResolveForDateAsync(viewModel.FacilityId, editReportDate)
            : null;
        viewModel.FacilityPermitId = resolvedEditPermit?.Id;
        viewModel.FacilityPermitDisplay = resolvedEditPermit != null ? $"{resolvedEditPermit.PermitNumber} v{resolvedEditPermit.PermitVersion}" : null;
        if (!viewModel.TemplateParameters.Any())
        {
            viewModel.TemplateParameters = await BuildWwCharTemplateInputsAsync(viewModel.CompanyId, viewModel.FacilityId, resolvedEditPermit?.Id, editReportDate, viewModel.Id);
        }
        viewModel.TemplateParametersStatusMessage = await BuildWwCharTemplateStatusMessageAsync(
            viewModel.CompanyId,
            viewModel.FacilityId,
            resolvedEditPermit?.Id,
            editReportDate,
            viewModel.TemplateParameters.Count);
        EnsureTemplateArraysInitialized(viewModel.TemplateParameters);

        if (!ModelState.IsValid)
        {
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Months = GetMonthSelectList();
            ViewBag.ORCOnSiteOptions = GetORCOnSiteSelectList();
            ViewBag.FlowMeasuringPointOptions = GetFlowMeasuringPointSelectList();
            ViewBag.ParameterMonitoringPointOptions = GetParameterMonitoringPointSelectList();
            return View(viewModel);
        }

        try
        {
            var wwChar = await _wwCharService.GetByIdAsync(viewModel.Id);
            if (wwChar == null)
                return NotFound();

            wwChar.Month = viewModel.Month;
            wwChar.Year = viewModel.Year;
            wwChar.BOD5Daily = viewModel.BOD5Daily;
            wwChar.TSSDaily = viewModel.TSSDaily;
            wwChar.FlowRateDaily = viewModel.FlowRateDaily;
            wwChar.PHDaily = viewModel.PHDaily;
            wwChar.NH3NDaily = viewModel.NH3NDaily;
            wwChar.FecalColiformDaily = viewModel.FecalColiformDaily;
            wwChar.ChlorideDaily = viewModel.ChlorideDaily;
            wwChar.CaDaily = viewModel.CaDaily;
            wwChar.MgDaily = viewModel.MgDaily;
            wwChar.NaDaily = viewModel.NaDaily;
            wwChar.SARDaily = viewModel.SARDaily;
            wwChar.TNDaily = viewModel.TNDaily;
            wwChar.CompositeTime = viewModel.CompositeTime;
            wwChar.ORCOnSite = viewModel.ORCOnSite;
            wwChar.LagoonFreeboard = viewModel.LagoonFreeboard;
            wwChar.LabCertification = viewModel.LabCertification;
            wwChar.CollectedBy = viewModel.CollectedBy;
            wwChar.AnalyzedBy = viewModel.AnalyzedBy;
            wwChar.FacilityPermitId = viewModel.FacilityPermitId;
            wwChar.FlowMeasuringPoint = viewModel.FlowMeasuringPoint;
            wwChar.ParameterMonitoringPoint = viewModel.ParameterMonitoringPoint;
            ApplyLegacyChemistrySnapshotsFromTemplate(wwChar, viewModel.TemplateParameters);

            await _wwCharService.UpdateAsync(wwChar);
            await SaveWwCharTemplateValuesAsync(wwChar, viewModel.TemplateParameters);
            TempData["SuccessMessage"] = $"Wastewater characteristics record updated for {wwChar.Month} {wwChar.Year}.";
            return RedirectToAction(nameof(WWChars), new { facilityId = wwChar.FacilityId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Months = GetMonthSelectList();
            ViewBag.ORCOnSiteOptions = GetORCOnSiteSelectList();
            ViewBag.FlowMeasuringPointOptions = GetFlowMeasuringPointSelectList();
            ViewBag.ParameterMonitoringPointOptions = GetParameterMonitoringPointSelectList();
            if (!viewModel.TemplateParameters.Any())
            {
                viewModel.TemplateParameters = await BuildWwCharTemplateInputsAsync(viewModel.CompanyId, viewModel.FacilityId, resolvedEditPermit?.Id, editReportDate, viewModel.Id);
            }
            viewModel.TemplateParametersStatusMessage = await BuildWwCharTemplateStatusMessageAsync(
                viewModel.CompanyId,
                viewModel.FacilityId,
                resolvedEditPermit?.Id,
                editReportDate,
                viewModel.TemplateParameters.Count);
            return View(viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireTechnician)]
    public async Task<IActionResult> WWCharDelete(Guid id)
    {
        try
        {
            var wwChar = await _wwCharService.GetByIdAsync(id);
            if (wwChar != null)
            {
                await EnsureCompanyAccessAsync(wwChar.CompanyId);
            }

            await _wwCharService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Wastewater characteristics record deleted successfully.";
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "Wastewater characteristics record not found.";
        }

        return RedirectToAction(nameof(WWChars));
    }

    #endregion

    #region Groundwater Monitoring (GWMonit)

    [HttpGet]
    public async Task<IActionResult> GWMonits(Guid? companyId = null, Guid? facilityId = null, Guid? monitoringWellId = null)
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

        var gwMonits = await _gwMonitService.GetAllAsync(companyId, facilityId, monitoringWellId);
        
        var viewModels = gwMonits.Select(g => new GWMonitViewModel
        {
            Id = g.Id,
            CompanyId = g.CompanyId,
            CompanyName = g.Company?.Name,
            FacilityId = g.FacilityId,
            FacilityName = g.Facility?.Name,
            MonitoringWellId = g.MonitoringWellId,
            MonitoringWellName = g.MonitoringWell?.WellId,
            SampleDate = g.SampleDate,
            SampleDepth = g.SampleDepth,
            WaterLevel = g.WaterLevel,
            Temperature = g.Temperature,
            PH = g.PH,
            GallonsPumped = g.GallonsPumped,
            Odor = g.Odor,
            Appearance = g.Appearance,
            Conductivity = g.Conductivity,
            TDS = g.TDS,
            Turbidity = g.Turbidity,
            TSS = g.TSS,
            NH3N = g.NH3N,
            NO3N = g.NO3N,
            TKN = g.TKN,
            TOC = g.TOC,
            Chloride = g.Chloride,
            Calcium = g.Calcium,
            Magnesium = g.Magnesium,
            MetalsSamplesCollectedUnfiltered = g.MetalsSamplesCollectedUnfiltered ?? false,
            MetalSamplesFieldAcidified = g.MetalSamplesFieldAcidified ?? false,
            FecalColiform = g.FecalColiform,
            TotalColiform = g.TotalColiform,
            VOCReportAttached = g.VOCReportAttached ?? false,
            VOCMethodNumber = g.VOCMethodNumber,
            LabCertification = g.LabCertification,
            CollectedBy = g.CollectedBy,
            AnalyzedBy = g.AnalyzedBy,
            Comments = g.Comments
        });

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        ViewBag.Facilities = await GetFacilitySelectListAsync(companyId);
        ViewBag.MonitoringWells = await GetMonitoringWellSelectListAsync(companyId, facilityId);
        ViewBag.SelectedCompanyId = companyId;
        ViewBag.SelectedFacilityId = facilityId;
        ViewBag.SelectedMonitoringWellId = monitoringWellId;

        return View(viewModels);
    }

    [HttpGet]
    public async Task<IActionResult> GWMonitDetails(Guid id)
    {
        var gwMonit = await _gwMonitService.GetByIdAsync(id);
        if (gwMonit == null)
            return NotFound();

        await EnsureCompanyAccessAsync(gwMonit.CompanyId);

        var viewModel = new GWMonitViewModel
        {
            Id = gwMonit.Id,
            CompanyId = gwMonit.CompanyId,
            CompanyName = gwMonit.Company?.Name,
            FacilityId = gwMonit.FacilityId,
            FacilityName = gwMonit.Facility?.Name,
            MonitoringWellId = gwMonit.MonitoringWellId,
            MonitoringWellName = gwMonit.MonitoringWell?.WellId,
            SampleDate = gwMonit.SampleDate,
            SampleDepth = gwMonit.SampleDepth,
            WaterLevel = gwMonit.WaterLevel,
            Temperature = gwMonit.Temperature,
            PH = gwMonit.PH,
            GallonsPumped = gwMonit.GallonsPumped,
            Odor = gwMonit.Odor,
            Appearance = gwMonit.Appearance,
            Conductivity = gwMonit.Conductivity,
            TDS = gwMonit.TDS,
            Turbidity = gwMonit.Turbidity,
            TSS = gwMonit.TSS,
            NH3N = gwMonit.NH3N,
            NO3N = gwMonit.NO3N,
            TKN = gwMonit.TKN,
            TOC = gwMonit.TOC,
            Chloride = gwMonit.Chloride,
            Calcium = gwMonit.Calcium,
            Magnesium = gwMonit.Magnesium,
            MetalsSamplesCollectedUnfiltered = gwMonit.MetalsSamplesCollectedUnfiltered ?? false,
            MetalSamplesFieldAcidified = gwMonit.MetalSamplesFieldAcidified ?? false,
            FecalColiform = gwMonit.FecalColiform,
            TotalColiform = gwMonit.TotalColiform,
            VOCReportAttached = gwMonit.VOCReportAttached ?? false,
            VOCMethodNumber = gwMonit.VOCMethodNumber,
            LabCertification = gwMonit.LabCertification,
            CollectedBy = gwMonit.CollectedBy,
            AnalyzedBy = gwMonit.AnalyzedBy,
            Comments = gwMonit.Comments
        };

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireTechnician)]
    public async Task<IActionResult> GWMonitReport(Guid id)
    {
        var model = await BuildGW59ReportAsync(id);
        return View(model);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireTechnician)]
    public async Task<IActionResult> GWMonitReportPdf(Guid id)
    {
        var reportModel = await BuildGW59ReportAsync(id);

        var templatePath = Path.Combine(
            _environment.WebRootPath,
            "forms",
            "GW-59 GW-QualityMonitoringReportForm.pdf");

        if (!System.IO.File.Exists(templatePath))
        {
            return NotFound("GW-59 template PDF not found.");
        }

        using var outputStream = new MemoryStream();
        using (var document = PdfReader.Open(templatePath, PdfDocumentOpenMode.Modify))
        {
            var page = document.Pages[0];
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 8, XFontStyle.Regular);

            void DrawText(string? text, double x, double y)
            {
                gfx.DrawString(text ?? string.Empty, font, XBrushes.Black,
                    new XRect(x, y, 250, font.Height + 2),
                    XStringFormats.TopLeft);
            }

            // DEBUG GRID (temporary) - helps calibrate coordinates for fields.
            // Comment out or remove this block once you've recorded the positions you need.
            //for (int y = 50; y <= page.Height; y += 20)
            //{
            //    gfx.DrawLine(XPens.Red, 40, y, page.Width - 40, y);
            //    gfx.DrawString(y.ToString(), font, XBrushes.Red,
            //        new XRect(5, y - 4, 30, font.Height + 2), XStringFormats.TopLeft);
            //}

            //for (int x = 50; x <= page.Width - 40; x += 20)
            //{
            //    gfx.DrawLine(XPens.Red, x, 40, x, page.Height - 40);
            //    gfx.DrawString(x.ToString(), font, XBrushes.Red,
            //        new XRect(x - 10, 25, 40, font.Height + 2), XStringFormats.TopLeft);
            //}

            // NOTE: All coordinates are approximate and may need fine-tuning
            // Facility information
            DrawText(reportModel.FacilityName, 120, 74);                      // Facility Name
            DrawText(reportModel.PermitNumber, 630, 60);                      // Permit Number
            DrawText(reportModel.Permittee, 170, 90);                          // Permit Name
            DrawText($"{reportModel.Address}", 120, 106);                      // Address line
            DrawText(reportModel.City, 80, 140);
            DrawText(reportModel.ZipCode, 280, 140);
            DrawText(reportModel.State, 230, 140);
            DrawText(reportModel.County, 410, 120);                            // County

            // Permit expiration (small box near permit header on template)
            DrawText(reportModel.PermitExpirationDate?.ToString("MM/dd/yyyy"), 775, 60);

            // Contact / phone – using facility phone
            DrawText(reportModel.FacilityPhone, 410, 152);

            // Sampling information / well details
            DrawText(reportModel.WellId, 210, 204);                               // WELL ID NUMBER (from Permit)
            DrawText(reportModel.SampleDate.ToString("MM/dd/yyyy"), 460, 204);    // Date sample collected

            // Well location / site name (right-hand box in sampling section)
            DrawText(reportModel.WellLocation, 150, 168);

            DrawText(reportModel.WellDepthFeet?.ToString("F2"), 158, 222);        // Well Depth
            DrawText(reportModel.DiameterInches?.ToString("F2"), 455, 222);       // Well Diameter

            // Screened interval – combined string "{low} to {high} ft" placed on same row
            string? screenedInterval = null;
            if (reportModel.LowScreenDepthFeet.HasValue || reportModel.HighScreenDepthFeet.HasValue)
            {
                var low = reportModel.LowScreenDepthFeet?.ToString("F2") ?? "?";
                var high = reportModel.HighScreenDepthFeet?.ToString("F2") ?? "?";
                screenedInterval = $"{low} to {high} ft";
            }
            DrawText(screenedInterval, 260, 220);

            // NOTE: Sample depth is intentionally not drawn; the official GW-59 form
            // does not provide a dedicated field for sample depth.
            DrawText(reportModel.WaterLevel?.ToString("F2"), 160, 237);           // Depth to water

            DrawText(reportModel.GallonsPumped?.ToString("F2"), 260, 266);        // Volume pumped

            // Field analyses
            DrawText(reportModel.PHField?.ToString("F2"), 615, 220);     // pH field
            DrawText(reportModel.TemperatureField?.ToString("F1"), 750, 220); // Temp field
            DrawText(reportModel.SpecificConductance?.ToString("F2"), 660, 238); // Spec. Cond.
            DrawText(reportModel.Odor, 650, 252);                        // Odor
            DrawText(reportModel.Appearance, 650, 267);                  // Appearance

            // Metals handling YES/NO checkboxes (approximate positions)
            if (reportModel.MetalsUnfiltered)
            {
                DrawText("X", 240, 283); // YES box
            }
            else
            {
                DrawText("X", 301, 282); // NO box
            }

            if (reportModel.MetalsAcidified)
            {
                DrawText("X", 444, 283); // YES box for acidified
            }
            else
            {
                DrawText("X", 491, 283); // NO box for acidified
            }

            // Laboratory information
            DrawText(reportModel.LabName, 450, 308);                           // Laboratory Name
            DrawText(reportModel.LabCertificationNumber, 740, 308);           // Certification No.

            // Core parameters from GWMonit
            DrawText(reportModel.TDS?.ToString("F2"), 160, 400);               // Dissolved Solids: Total
            DrawText(reportModel.TOC?.ToString("F2"), 165, 430);               // TOC
            DrawText(reportModel.Chloride?.ToString("F2"), 165, 446);          // Chloride

            DrawText(reportModel.NH3N?.ToString("F2"), 165, 538);              // Total Ammonia
            DrawText(reportModel.TKN?.ToString("F2"), 165, 571);               // TKN as N

            DrawText(reportModel.NO3N?.ToString("F2"), 440, 352);              // Nitrate (NO3) as N

            DrawText(reportModel.Calcium?.ToString("F2"), 440, 430);           // Ca
            DrawText(reportModel.Magnesium?.ToString("F2"), 440, 538);         // Mg

            DrawText(reportModel.FecalColiform?.ToString("F0"), 165, 352);     // Coliform MF Fecal
            DrawText(reportModel.TotalColiform?.ToString("F0"), 165, 369);     // Coliform MF Total

            // Organics section – lab report + VOC method
            if (reportModel.LabReportAttached)
            {
                DrawText("X", 684, 510); // Yes box
            }
            else
            {
                DrawText("X", 752, 510); // No box
            }

            DrawText(reportModel.VOCMethodNumber, 750, 525);             // VOC method #

            // Certification block – name, title, date in signature area
            DrawText(reportModel.CertificationName, 140, 690);           // Printed name
            DrawText(reportModel.CertificationTitle, 140, 708);          // Title
            DrawText(reportModel.CertificationDate?.ToString("MM/dd/yyyy"), 140, 726); // Date

            document.Save(outputStream, false);
        }

        outputStream.Position = 0;
        var safeFacility = string.IsNullOrWhiteSpace(reportModel.FacilityName)
            ? "Facility"
            : reportModel.FacilityName.Replace(' ', '_');
        var fileName = $"GW59_{safeFacility}_{reportModel.SampleDate:yyyyMMdd}.pdf";
        return File(outputStream.ToArray(), "application/pdf", fileName);
    }

    private async Task<GW59ReportViewModel> BuildGW59ReportAsync(Guid gwMonitId)
    {
        var gwMonit = await _gwMonitService.GetByIdAsync(gwMonitId);
        if (gwMonit == null)
        {
            throw new Infrastructure.Exceptions.EntityNotFoundException(nameof(GWMonit), gwMonitId);
        }

        await EnsureCompanyAccessAsync(gwMonit.CompanyId);

        var facility = gwMonit.Facility;
        var well = gwMonit.MonitoringWell;

        // Basic mapping from entities to report view model
        var report = new GW59ReportViewModel
        {
            GwMonitId = gwMonit.Id,
            FacilityId = gwMonit.FacilityId,
            FacilityName = facility?.Name ?? string.Empty,
            PermitNumber = facility?.PermitNumber ?? string.Empty,
            Permittee = facility?.Permittee ?? string.Empty,
            Address = facility?.Address ?? string.Empty,
            City = facility?.City ?? string.Empty,
            State = facility?.State ?? string.Empty,
            ZipCode = facility?.ZipCode ?? string.Empty,
            County = facility?.County ?? string.Empty,
            FacilityPhone = facility?.FacilityPhone ?? string.Empty,
            PermitExpirationDate = facility?.PermitExpirationDate,

            MonitoringWellId = gwMonit.MonitoringWellId,
            WellId = well?.WellId ?? string.Empty,
            WellLocation = well?.LocationDescription ?? string.Empty,
            WellDepthFeet = well?.WellDepthFeet,
            DiameterInches = well?.DiameterInches,
            LowScreenDepthFeet = well?.LowScreenDepthFeet,
            HighScreenDepthFeet = well?.HighScreenDepthFeet,
            NumberOfWellsToBeSampled = well?.NumberOfWellsToBeSampled,

            SampleDate = gwMonit.SampleDate,
            SampleDepth = gwMonit.SampleDepth,
            WaterLevel = gwMonit.WaterLevel,
            GallonsPumped = gwMonit.GallonsPumped,
            PHField = gwMonit.PH,
            TemperatureField = gwMonit.Temperature,
            SpecificConductance = gwMonit.Conductivity,
            Odor = gwMonit.Odor,
            Appearance = gwMonit.Appearance,
            MetalsUnfiltered = gwMonit.MetalsSamplesCollectedUnfiltered ?? false,
            MetalsAcidified = gwMonit.MetalSamplesFieldAcidified ?? false,

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
                ? (facility?.CertifiedLaboratory1Name ?? string.Empty)
                : gwMonit.AnalyzedBy,
            LabCertificationNumber = string.IsNullOrWhiteSpace(gwMonit.LabCertification)
                ? (facility?.LabCertificationNumber1 ?? string.Empty)
                : gwMonit.LabCertification,
            LabReportAttached = gwMonit.VOCReportAttached ?? false,
            VOCMethodNumber = gwMonit.VOCMethodNumber
        };

        // Certification block – default name from current user if available
        var currentUser = await GetCurrentUserAsync();
        if (currentUser != null)
        {
            report.CertificationName = string.IsNullOrWhiteSpace(currentUser.FullName)
                ? currentUser.UserName ?? string.Empty
                : currentUser.FullName;
        }

        return report;
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireTechnician)]
    public async Task<IActionResult> GWMonitCreate(Guid? companyId = null, Guid? facilityId = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // If a non-global user, default to their company when no company is specified
        // Set company ID if not provided (respects session selection for admins)
        if (!companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        // If facility is selected but companyId is still unknown (e.g., global admin),
        // derive the company from the facility so downstream logic has a valid company.
        if (!companyId.HasValue && facilityId.HasValue)
        {
            var facility = await _facilityService.GetByIdAsync(facilityId.Value);
            if (facility != null)
            {
                companyId = facility.CompanyId;
            }
        }

        if (companyId.HasValue)
        {
            await EnsureCompanyAccessAsync(companyId.Value);
        }

        var viewModel = new GWMonitCreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty,
            FacilityId = facilityId ?? Guid.Empty,
            SampleDate = DateTime.Today
        };

        if (viewModel.FacilityId != Guid.Empty)
        {
            var resolvedPermit = await _facilityPermitResolver.ResolveForDateAsync(viewModel.FacilityId, viewModel.SampleDate);
            viewModel.FacilityPermitId = resolvedPermit?.Id;
            viewModel.FacilityPermitDisplay = resolvedPermit == null
                ? null
                : $"{resolvedPermit.PermitNumber} v{resolvedPermit.PermitVersion}";
            viewModel.TemplateParameters = await BuildGwMonitTemplateInputsAsync(viewModel.FacilityId, resolvedPermit?.Id, viewModel.SampleDate);
            viewModel.TemplateParametersStatusMessage = await BuildGwMonitTemplateStatusMessageAsync(
                viewModel.CompanyId,
                viewModel.FacilityId,
                resolvedPermit?.Id,
                viewModel.SampleDate,
                viewModel.TemplateParameters.Count);
        }

        ViewBag.Facilities = await GetFacilitySelectListAsync(companyId);
        ViewBag.MonitoringWells = await GetMonitoringWellSelectListAsync(companyId, facilityId);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireTechnician)]
    public async Task<IActionResult> GWMonitCreate(GWMonitCreateViewModel viewModel)
    {
        var facility = await _facilityService.GetByIdAsync(viewModel.FacilityId);
        if (facility == null)
        {
            ModelState.AddModelError("FacilityId", "Selected facility was not found.");
        }

        var resolvedPermit = viewModel.FacilityId == Guid.Empty
            ? null
            : await _facilityPermitResolver.ResolveForDateAsync(viewModel.FacilityId, viewModel.SampleDate);
        viewModel.FacilityPermitId = resolvedPermit?.Id;
        viewModel.FacilityPermitDisplay = resolvedPermit == null
            ? null
            : $"{resolvedPermit.PermitNumber} v{resolvedPermit.PermitVersion}";

        if (viewModel.FacilityId != Guid.Empty)
        {
            viewModel.TemplateParameters = await BuildGwMonitTemplateInputsForPostAsync(
                viewModel.FacilityId,
                resolvedPermit?.Id,
                viewModel.SampleDate,
                viewModel.TemplateParameters);
        }
        else
        {
            viewModel.TemplateParameters = new List<GWMonitTemplateParameterViewModel>();
        }

        var missingTemplateRows = ValidateGwTemplateRequirements(viewModel.TemplateParameters);
        if (missingTemplateRows.Count > 0)
        {
            var joinedRows = string.Join(", ", missingTemplateRows);
            ModelState.AddModelError(
                string.Empty,
                $"We could not save this record yet because required permit-template values for {viewModel.SampleDate:MMM yyyy} are missing: {joinedRows}. Enter values for these required rows to unblock save.");
        }

        if (facility != null)
        {
            viewModel.CompanyId = facility.CompanyId;
            await EnsureCompanyAccessAsync(facility.CompanyId);
        }

        if (!ModelState.IsValid)
        {
            var companyIdForLists = facility?.CompanyId != Guid.Empty ? facility?.CompanyId : viewModel.CompanyId;
            viewModel.TemplateParametersStatusMessage = await BuildGwMonitTemplateStatusMessageAsync(
                companyIdForLists ?? Guid.Empty,
                viewModel.FacilityId,
                resolvedPermit?.Id,
                viewModel.SampleDate,
                viewModel.TemplateParameters.Count);
            ViewBag.Facilities = await GetFacilitySelectListAsync(companyIdForLists);
            ViewBag.MonitoringWells = await GetMonitoringWellSelectListAsync(companyIdForLists, viewModel.FacilityId);
            return View(viewModel);
        }

        try
        {
            var gwMonit = new GWMonit
            {
                CompanyId = facility.CompanyId,
                FacilityId = viewModel.FacilityId,
                MonitoringWellId = viewModel.MonitoringWellId,
                SampleDate = viewModel.SampleDate,
                SampleDepth = viewModel.SampleDepth,
                WaterLevel = viewModel.WaterLevel,
                Temperature = viewModel.Temperature,
                GallonsPumped = viewModel.GallonsPumped,
                Odor = viewModel.Odor,
                Appearance = viewModel.Appearance,
                Conductivity = viewModel.Conductivity,
                TDS = viewModel.TDS,
                Turbidity = viewModel.Turbidity,
                TOC = viewModel.TOC,
                MetalsSamplesCollectedUnfiltered = viewModel.MetalsSamplesCollectedUnfiltered,
                MetalSamplesFieldAcidified = viewModel.MetalSamplesFieldAcidified,
                VOCReportAttached = viewModel.VOCReportAttached,
                VOCMethodNumber = viewModel.VOCMethodNumber,
                LabCertification = viewModel.LabCertification,
                CollectedBy = viewModel.CollectedBy,
                AnalyzedBy = viewModel.AnalyzedBy,
                Comments = viewModel.Comments
            };

            await _gwMonitService.CreateAsync(gwMonit);
            await SaveGwMonitTemplateValuesAsync(gwMonit, viewModel.TemplateParameters);
            TempData["SuccessMessage"] = "Groundwater monitoring record created successfully.";
            return RedirectToAction(nameof(GWMonits), new { facilityId = gwMonit.FacilityId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            viewModel.TemplateParametersStatusMessage = await BuildGwMonitTemplateStatusMessageAsync(
                viewModel.CompanyId,
                viewModel.FacilityId,
                resolvedPermit?.Id,
                viewModel.SampleDate,
                viewModel.TemplateParameters.Count);
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.MonitoringWells = await GetMonitoringWellSelectListAsync(viewModel.CompanyId, viewModel.FacilityId);
            return View(viewModel);
        }
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireTechnician)]
    public async Task<IActionResult> GWMonitEdit(Guid id)
    {
        var gwMonit = await _gwMonitService.GetByIdAsync(id);
        if (gwMonit == null)
            return NotFound();

        await EnsureCompanyAccessAsync(gwMonit.CompanyId);

        var viewModel = new GWMonitEditViewModel
        {
            Id = gwMonit.Id,
            CompanyId = gwMonit.CompanyId,
            FacilityId = gwMonit.FacilityId,
            MonitoringWellId = gwMonit.MonitoringWellId,
            SampleDate = gwMonit.SampleDate,
            SampleDepth = gwMonit.SampleDepth,
            WaterLevel = gwMonit.WaterLevel,
            Temperature = gwMonit.Temperature,
            PH = gwMonit.PH,
            GallonsPumped = gwMonit.GallonsPumped,
            Odor = gwMonit.Odor,
            Appearance = gwMonit.Appearance,
            Conductivity = gwMonit.Conductivity,
            TDS = gwMonit.TDS,
            Turbidity = gwMonit.Turbidity,
            TSS = gwMonit.TSS,
            NH3N = gwMonit.NH3N,
            NO3N = gwMonit.NO3N,
            TKN = gwMonit.TKN,
            TOC = gwMonit.TOC,
            Chloride = gwMonit.Chloride,
            Calcium = gwMonit.Calcium,
            Magnesium = gwMonit.Magnesium,
            MetalsSamplesCollectedUnfiltered = gwMonit.MetalsSamplesCollectedUnfiltered ?? false,
            MetalSamplesFieldAcidified = gwMonit.MetalSamplesFieldAcidified ?? false,
            FecalColiform = gwMonit.FecalColiform,
            TotalColiform = gwMonit.TotalColiform,
            VOCReportAttached = gwMonit.VOCReportAttached ?? false,
            VOCMethodNumber = gwMonit.VOCMethodNumber,
            LabCertification = gwMonit.LabCertification,
            CollectedBy = gwMonit.CollectedBy,
            AnalyzedBy = gwMonit.AnalyzedBy,
            Comments = gwMonit.Comments
        };

        var resolvedEditPermit = await _facilityPermitResolver.ResolveForDateAsync(gwMonit.FacilityId, gwMonit.SampleDate);
        viewModel.FacilityPermitId = resolvedEditPermit?.Id;
        viewModel.FacilityPermitDisplay = resolvedEditPermit == null
            ? null
            : $"{resolvedEditPermit.PermitNumber} v{resolvedEditPermit.PermitVersion}";
        viewModel.TemplateParameters = await BuildGwMonitTemplateInputsAsync(gwMonit.FacilityId, resolvedEditPermit?.Id, gwMonit.SampleDate, gwMonit.Id);
        viewModel.TemplateParametersStatusMessage = await BuildGwMonitTemplateStatusMessageAsync(
            gwMonit.CompanyId,
            gwMonit.FacilityId,
            resolvedEditPermit?.Id,
            gwMonit.SampleDate,
            viewModel.TemplateParameters.Count);

        ViewBag.Facilities = await GetFacilitySelectListAsync(gwMonit.CompanyId);
        ViewBag.MonitoringWells = await GetMonitoringWellSelectListAsync(gwMonit.CompanyId, gwMonit.FacilityId);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireTechnician)]
    public async Task<IActionResult> GWMonitEdit(GWMonitEditViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        var resolvedPermit = viewModel.FacilityId == Guid.Empty
            ? null
            : await _facilityPermitResolver.ResolveForDateAsync(viewModel.FacilityId, viewModel.SampleDate);
        viewModel.FacilityPermitId = resolvedPermit?.Id;
        viewModel.FacilityPermitDisplay = resolvedPermit == null
            ? null
            : $"{resolvedPermit.PermitNumber} v{resolvedPermit.PermitVersion}";
        viewModel.TemplateParameters = viewModel.FacilityId == Guid.Empty
            ? new List<GWMonitTemplateParameterViewModel>()
            : await BuildGwMonitTemplateInputsForPostAsync(
                viewModel.FacilityId,
                resolvedPermit?.Id,
                viewModel.SampleDate,
                viewModel.TemplateParameters,
                viewModel.Id);

        var missingTemplateRows = ValidateGwTemplateRequirements(viewModel.TemplateParameters);
        if (missingTemplateRows.Count > 0)
        {
            var joinedRows = string.Join(", ", missingTemplateRows);
            ModelState.AddModelError(
                string.Empty,
                $"We could not save this record yet because required permit-template values for {viewModel.SampleDate:MMM yyyy} are missing: {joinedRows}. Enter values for these required rows to unblock save.");
        }

        if (!ModelState.IsValid)
        {
            viewModel.TemplateParametersStatusMessage = await BuildGwMonitTemplateStatusMessageAsync(
                viewModel.CompanyId,
                viewModel.FacilityId,
                resolvedPermit?.Id,
                viewModel.SampleDate,
                viewModel.TemplateParameters.Count);
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.MonitoringWells = await GetMonitoringWellSelectListAsync(viewModel.CompanyId, viewModel.FacilityId);
            return View(viewModel);
        }

        try
        {
            var gwMonit = await _gwMonitService.GetByIdAsync(viewModel.Id);
            if (gwMonit == null)
                return NotFound();

            gwMonit.SampleDate = viewModel.SampleDate;
            gwMonit.SampleDepth = viewModel.SampleDepth;
            gwMonit.WaterLevel = viewModel.WaterLevel;
            gwMonit.Temperature = viewModel.Temperature;
            gwMonit.GallonsPumped = viewModel.GallonsPumped;
            gwMonit.Odor = viewModel.Odor;
            gwMonit.Appearance = viewModel.Appearance;
            gwMonit.Conductivity = viewModel.Conductivity;
            gwMonit.TDS = viewModel.TDS;
            gwMonit.Turbidity = viewModel.Turbidity;
            gwMonit.TOC = viewModel.TOC;
            gwMonit.MetalsSamplesCollectedUnfiltered = viewModel.MetalsSamplesCollectedUnfiltered;
            gwMonit.MetalSamplesFieldAcidified = viewModel.MetalSamplesFieldAcidified;
            gwMonit.VOCReportAttached = viewModel.VOCReportAttached;
            gwMonit.VOCMethodNumber = viewModel.VOCMethodNumber;
            gwMonit.LabCertification = viewModel.LabCertification;
            gwMonit.CollectedBy = viewModel.CollectedBy;
            gwMonit.AnalyzedBy = viewModel.AnalyzedBy;
            gwMonit.Comments = viewModel.Comments;

            await _gwMonitService.UpdateAsync(gwMonit);
            await SaveGwMonitTemplateValuesAsync(gwMonit, viewModel.TemplateParameters);
            TempData["SuccessMessage"] = "Groundwater monitoring record updated successfully.";
            return RedirectToAction(nameof(GWMonits), new { facilityId = gwMonit.FacilityId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            viewModel.TemplateParametersStatusMessage = await BuildGwMonitTemplateStatusMessageAsync(
                viewModel.CompanyId,
                viewModel.FacilityId,
                resolvedPermit?.Id,
                viewModel.SampleDate,
                viewModel.TemplateParameters.Count);
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.MonitoringWells = await GetMonitoringWellSelectListAsync(viewModel.CompanyId, viewModel.FacilityId);
            return View(viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireTechnician)]
    public async Task<IActionResult> GWMonitDelete(Guid id)
    {
        try
        {
            var gwMonit = await _gwMonitService.GetByIdAsync(id);
            if (gwMonit != null)
            {
                await EnsureCompanyAccessAsync(gwMonit.CompanyId);
            }

            await _gwMonitService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Groundwater monitoring record deleted successfully.";
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "Groundwater monitoring record not found.";
        }

        return RedirectToAction(nameof(GWMonits));
    }

    #endregion

    #region Helper Methods

    private async Task<SelectList> GetFacilitySelectListAsync(Guid? companyId = null)
    {
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // Use effective company ID if no companyId specified (respects session selection for admins)
        if (!companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        var facilities = await _lookupQueryService.GetFacilitiesAsync(companyId);
        return new SelectList(facilities, "Id", "Name");
    }

    private async Task<SelectList> GetSprayfieldSelectListAsync(Guid? companyId = null, Guid? facilityId = null)
    {
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // Use effective company ID if no companyId specified (respects session selection for admins)
        if (!companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        var sprayfields = (await _lookupQueryService.GetSprayfieldsAsync(companyId, facilityId))
            .OrderBy(s => BuildNaturalSortKey(s.FieldId))
            .ThenBy(s => s.FieldId)
            .ToList();

        var items = sprayfields.Select(s => new SelectListItem
        {
            Value = s.Id.ToString(),
            Text = s.FieldId
        }).ToList();

        return new SelectList(items, "Value", "Text");
    }

    private static string BuildNaturalSortKey(string? input)
    {
        return Regex.Replace(input ?? string.Empty, @"\d+", match => match.Value.PadLeft(10, '0'));
    }

    private async Task<SelectList> GetCompanySelectListAsync()
    {
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();
        var companies = await _lookupQueryService.GetCompaniesAsync(effectiveCompanyId);

        return new SelectList(companies, "Id", "Name");
    }

    private async Task<SelectList> GetMonitoringWellSelectListAsync(Guid? companyId = null, Guid? facilityId = null)
    {
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // Use effective company ID if no companyId specified (respects session selection for admins)
        if (!companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        var monitoringWells = await _lookupQueryService.GetMonitoringWellsAsync(companyId, facilityId);
        return new SelectList(monitoringWells, "Id", "WellId");
    }

    private SelectList GetMonthSelectList()
    {
        return new SelectList(Enum.GetValues(typeof(MonthEnum)).Cast<MonthEnum>()
            .Select(e => new SelectListItem
            {
                Value = ((int)e).ToString(),
                Text = e.ToString()
            }), "Value", "Text");
    }

    private SelectList GetORCOnSiteSelectList()
    {
        return new SelectList(Enum.GetValues(typeof(ORCOnSiteEnum)).Cast<ORCOnSiteEnum>()
            .Select(e => new SelectListItem
            {
                Value = e.ToString(),
                Text = e.ToString()
            }), "Value", "Text");
    }

    private SelectList GetFlowMeasuringPointSelectList()
    {
        return new SelectList(Enum.GetValues(typeof(FlowMeasuringPointEnum)).Cast<FlowMeasuringPointEnum>()
            .Select(e => new SelectListItem
            {
                Value = e.ToString(),
                Text = e switch
                {
                    FlowMeasuringPointEnum.NoFlowGenerated => "No flow generated",
                    _ => e.ToString()
                }
            }), "Value", "Text");
    }

    private SelectList GetParameterMonitoringPointSelectList()
    {
        return new SelectList(Enum.GetValues(typeof(ParameterMonitoringPointEnum)).Cast<ParameterMonitoringPointEnum>()
            .Select(e => new SelectListItem
            {
                Value = e.ToString(),
                Text = e switch
                {
                    ParameterMonitoringPointEnum.GroundwaterLowering => "Groundwater Lowering",
                    ParameterMonitoringPointEnum.SurfaceWater => "Surface Water",
                    _ => e.ToString()
                }
            }), "Value", "Text");
    }

    private void InitializeDailyArrays(WWCharCreateViewModel viewModel)
    {
        EnsureDailyArraysInitialized(viewModel);
    }

    private void EnsureDailyArraysInitialized(WWCharCreateViewModel viewModel)
    {
        EnsureArraySize(viewModel.BOD5Daily, 31);
        EnsureArraySize(viewModel.TSSDaily, 31);
        EnsureArraySize(viewModel.FlowRateDaily, 31);
        EnsureArraySize(viewModel.PHDaily, 31);
        EnsureArraySize(viewModel.NH3NDaily, 31);
        EnsureArraySize(viewModel.FecalColiformDaily, 31);
        EnsureArraySize(viewModel.ChlorideDaily, 31);
        EnsureArraySize(viewModel.CaDaily, 31);
        EnsureArraySize(viewModel.MgDaily, 31);
        EnsureArraySize(viewModel.NaDaily, 31);
        EnsureArraySize(viewModel.SARDaily, 31);
        EnsureArraySize(viewModel.TNDaily, 31);
        EnsureStringArraySize(viewModel.CompositeTime, 31);
        EnsureEnumArraySize(viewModel.ORCOnSite, 31);
        EnsureArraySize(viewModel.LagoonFreeboard, 31);
    }

    private void EnsureDailyArraysInitialized(WWCharEditViewModel viewModel)
    {
        EnsureArraySize(viewModel.BOD5Daily, 31);
        EnsureArraySize(viewModel.TSSDaily, 31);
        EnsureArraySize(viewModel.FlowRateDaily, 31);
        EnsureArraySize(viewModel.PHDaily, 31);
        EnsureArraySize(viewModel.NH3NDaily, 31);
        EnsureArraySize(viewModel.FecalColiformDaily, 31);
        EnsureArraySize(viewModel.ChlorideDaily, 31);
        EnsureArraySize(viewModel.CaDaily, 31);
        EnsureArraySize(viewModel.MgDaily, 31);
        EnsureArraySize(viewModel.NaDaily, 31);
        EnsureArraySize(viewModel.SARDaily, 31);
        EnsureArraySize(viewModel.TNDaily, 31);
        EnsureStringArraySize(viewModel.CompositeTime, 31);
        EnsureEnumArraySize(viewModel.ORCOnSite, 31);
        EnsureArraySize(viewModel.LagoonFreeboard, 31);
    }

    private void EnsureDayArraysInitialized(WWCharTemplateSectionViewModel viewModel)
    {
        EnsureEnumArraySize(viewModel.ORCOnSite, 31);
        EnsureArraySize(viewModel.LagoonFreeboard, 31);
    }

    private void EnsureArraySize<T>(List<T> list, int size)
    {
        if (list == null)
        {
            list = new List<T>();
        }

        while (list.Count < size)
        {
            list.Add(default(T)!);
        }

        while (list.Count > size)
        {
            list.RemoveAt(list.Count - 1);
        }
    }

    private void EnsureStringArraySize(List<string?> list, int size)
    {
        if (list == null)
        {
            list = new List<string?>();
        }

        while (list.Count < size)
        {
            list.Add(null);
        }

        while (list.Count > size)
        {
            list.RemoveAt(list.Count - 1);
        }
    }

    private void EnsureEnumArraySize<T>(List<T?> list, int size) where T : struct, Enum
    {
        if (list == null)
        {
            list = new List<T?>();
        }

        while (list.Count < size)
        {
            list.Add(null);
        }

        while (list.Count > size)
        {
            list.RemoveAt(list.Count - 1);
        }
    }

    private string BuildMissingWwCharMessage(DateTime applicationDate)
    {
        return $"WWChar chemistry (including TKN) is required for {applicationDate:MMM yyyy} before saving this Monthly Application.";
    }

    private string BuildWwCharCreateShortcutUrl(Guid facilityId, DateTime applicationDate)
    {
        return Url.Action(
                   nameof(WWCharCreate),
                   new
                   {
                       facilityId,
                       month = applicationDate.Month,
                       year = applicationDate.Year
                   }) ?? string.Empty;
    }

    private static void EnsureTemplateArraysInitialized(List<WWCharTemplateParameterInputViewModel>? parameters)
    {
        if (parameters == null) return;
        foreach (var parameter in parameters)
        {
            while (parameter.DailyValues.Count < 31) parameter.DailyValues.Add(null);
            while (parameter.DailyValues.Count > 31) parameter.DailyValues.RemoveAt(parameter.DailyValues.Count - 1);
        }
    }

    private async Task<List<WWCharTemplateParameterInputViewModel>> BuildWwCharTemplateInputsAsync(
        Guid companyId,
        Guid facilityId,
        Guid? resolvedPermitId,
        DateTime reportDate,
        Guid? wwCharId)
    {
        var permitId = resolvedPermitId;
        if (!permitId.HasValue)
        {
            var permit = await _facilityPermitResolver.ResolveForDateAsync(facilityId, reportDate);
            permitId = permit?.Id;
        }

        if (!permitId.HasValue) return new List<WWCharTemplateParameterInputViewModel>();

        var templateRows = await _context.FacilityPermitTemplateParameters
            .Include(x => x.PcsParameterCatalog)
            .Where(x => x.FacilityPermitId == permitId.Value && (x.ReportTypes & PermitTemplateReportTypeEnum.Ndmr) != 0)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();
        templateRows = templateRows
            .Where(row => IsTemplateRowApplicableForMonth(row, reportDate.Month))
            .ToList();

        var existingValues = wwCharId.HasValue
            ? await _context.WWCharTemplateValues.Where(x => x.WWCharId == wwCharId.Value).ToListAsync()
            : new List<WWCharTemplateValue>();

        var result = new List<WWCharTemplateParameterInputViewModel>();
        foreach (var row in templateRows)
        {
            var vm = new WWCharTemplateParameterInputViewModel
            {
                FacilityPermitTemplateParameterId = row.Id,
                PcsCode = row.PcsParameterCatalog?.PcsCode ?? string.Empty,
                ParameterName = row.ParameterDisplayOverride ?? row.PcsParameterCatalog?.UserFriendlyName ?? row.PcsParameterCatalog?.OfficialParameterName ?? string.Empty,
                Units = row.UnitsOverride ?? row.PcsParameterCatalog?.AcceptedUnits ?? string.Empty,
                IsRequired = row.IsRequired,
                MeasurementFrequency = row.MeasurementFrequency.ToDisplayLabel(),
                SampleType = row.SampleType.ToString(),
                ScheduledMonthsCsv = row.ScheduledMonthsCsv,
                Notes = row.Notes,
                MonthlyAverageLimit = row.MonthlyAverageLimit,
                MonthlyGeometricMeanLimit = row.MonthlyGeometricMeanLimit,
                DailyMinimumLimit = row.DailyMinimumLimit,
                DailyMaximumLimit = row.DailyMaximumLimit,
                DailyValues = Enumerable.Repeat<decimal?>(null, 31).ToList()
            };

            foreach (var value in existingValues.Where(v => v.FacilityPermitTemplateParameterId == row.Id && v.DayNo >= 1 && v.DayNo <= 31))
            {
                vm.DailyValues[value.DayNo - 1] = value.NumericValue;
            }

            result.Add(vm);
        }

        return result;
    }

    private async Task SaveWwCharTemplateValuesAsync(WWChar wwChar, List<WWCharTemplateParameterInputViewModel>? templateParameters)
    {
        if (templateParameters == null) return;

        var existing = await _context.WWCharTemplateValues.Where(x => x.WWCharId == wwChar.Id).ToListAsync();
        _context.WWCharTemplateValues.RemoveRange(existing);

        foreach (var parameter in templateParameters)
        {
            for (var day = 1; day <= parameter.DailyValues.Count && day <= 31; day++)
            {
                var value = parameter.DailyValues[day - 1];
                if (!value.HasValue) continue;

                _context.WWCharTemplateValues.Add(new WWCharTemplateValue
                {
                    Id = Guid.NewGuid(),
                    CompanyId = wwChar.CompanyId,
                    WWCharId = wwChar.Id,
                    FacilityPermitTemplateParameterId = parameter.FacilityPermitTemplateParameterId,
                    DayNo = day,
                    NumericValue = value,
                    CreatedBy = User?.Identity?.Name ?? "System"
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    private List<string> ValidateGwTemplateRequirements(List<GWMonitTemplateParameterViewModel>? templateParameters)
    {
        if (templateParameters == null || templateParameters.Count == 0)
        {
            return new List<string>();
        }

        return templateParameters
            .Where(p => p.IsRequiredForSelectedMonth && !p.EnteredValue.HasValue)
            .Select(p => $"{p.PcsCode} - {p.ParameterName}")
            .ToList();
    }

    private async Task SaveGwMonitTemplateValuesAsync(GWMonit gwMonit, List<GWMonitTemplateParameterViewModel>? templateParameters)
    {
        if (templateParameters == null) return;

        var existing = await _context.GWMonitTemplateValues.Where(x => x.GWMonitId == gwMonit.Id).ToListAsync();
        _context.GWMonitTemplateValues.RemoveRange(existing);

        foreach (var parameter in templateParameters)
        {
            if (!parameter.EnteredValue.HasValue) continue;

            _context.GWMonitTemplateValues.Add(new GWMonitTemplateValue
            {
                Id = Guid.NewGuid(),
                CompanyId = gwMonit.CompanyId,
                GWMonitId = gwMonit.Id,
                FacilityPermitTemplateParameterId = parameter.FacilityPermitTemplateParameterId,
                NumericValue = parameter.EnteredValue,
                CreatedBy = User?.Identity?.Name ?? "System"
            });
        }

        await _context.SaveChangesAsync();
    }

    private async Task<List<GWMonitTemplateParameterViewModel>> BuildGwMonitTemplateInputsForPostAsync(
        Guid facilityId,
        Guid? resolvedPermitId,
        DateTime sampleDate,
        List<GWMonitTemplateParameterViewModel>? postedParameters,
        Guid? gwMonitId = null)
    {
        var rows = await BuildGwMonitTemplateInputsAsync(facilityId, resolvedPermitId, sampleDate, gwMonitId);
        if (postedParameters == null || postedParameters.Count == 0)
        {
            return rows;
        }

        var postedByTemplateId = postedParameters.ToDictionary(x => x.FacilityPermitTemplateParameterId, x => x.EnteredValue);
        foreach (var row in rows)
        {
            if (postedByTemplateId.TryGetValue(row.FacilityPermitTemplateParameterId, out var enteredValue))
            {
                row.EnteredValue = enteredValue;
            }
        }

        return rows;
    }

    private static bool IsTemplateRowApplicableForMonth(FacilityPermitTemplateParameter row, int month)
    {
        var alwaysInclude = row.MeasurementFrequency is MeasurementFrequencyEnum.Daily
            or MeasurementFrequencyEnum.Weekly
            or MeasurementFrequencyEnum.Monthly
            or MeasurementFrequencyEnum.Continuous;
        if (alwaysInclude)
        {
            return true;
        }

        // Strict mode for periodic/unspecified rows: only include when schedule explicitly includes the month.
        if (string.IsNullOrWhiteSpace(row.ScheduledMonthsCsv))
        {
            return false;
        }

        var months = row.ScheduledMonthsCsv
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(value => int.TryParse(value, out var parsed) ? parsed : -1);
        return months.Any(m => m == month);
    }

    private static void ApplyLegacyChemistrySnapshotsFromTemplate(
        WWChar wwChar,
        List<WWCharTemplateParameterInputViewModel>? templateParameters)
    {
        if (templateParameters == null || templateParameters.Count == 0)
        {
            wwChar.NO2N = 0m;
            return;
        }

        var tknRow = templateParameters.FirstOrDefault(p => string.Equals(p.PcsCode, WWCharChemistryResolver.TknPcsCode, StringComparison.OrdinalIgnoreCase));
        var no3Row = templateParameters.FirstOrDefault(p => string.Equals(p.PcsCode, WWCharChemistryResolver.No3PcsCode, StringComparison.OrdinalIgnoreCase));
        var nh3Row = templateParameters.FirstOrDefault(p => string.Equals(p.PcsCode, "00610", StringComparison.OrdinalIgnoreCase));

        var tknAverage = tknRow == null ? null : WWCharChemistryResolver.AverageNonNull(tknRow.DailyValues);
        var no3Average = no3Row == null ? null : WWCharChemistryResolver.AverageNonNull(no3Row.DailyValues);

        wwChar.NO2N = 0m;
        if (tknAverage.HasValue)
        {
            wwChar.TKNN = tknAverage.Value;
        }

        if (no3Average.HasValue)
        {
            wwChar.NO3N = no3Average.Value;
        }

        if (nh3Row != null)
        {
            wwChar.NH3NDaily = nh3Row.DailyValues.Take(31).ToList();
            while (wwChar.NH3NDaily.Count < 31)
            {
                wwChar.NH3NDaily.Add(null);
            }
        }
    }

    private async Task<string?> BuildWwCharTemplateStatusMessageAsync(
        Guid companyId,
        Guid facilityId,
        Guid? resolvedPermitId,
        DateTime reportDate,
        int templateParameterCount)
    {
        if (templateParameterCount > 0) return null;
        if (facilityId == Guid.Empty) return "Select a facility to load permit template parameters.";

        var anyPermitsForFacility = await _context.FacilityPermits
            .AnyAsync(x => x.CompanyId == companyId && x.FacilityId == facilityId && !x.IsDeleted);

        if (!anyPermitsForFacility)
        {
            return "No permit versions are configured for this facility. Add a permit version first in System Administration > Facilities > Permit Versions.";
        }

        FacilityPermit? permit = null;
        if (resolvedPermitId.HasValue)
        {
            permit = await _context.FacilityPermits
                .Where(x => x.Id == resolvedPermitId.Value && !x.IsDeleted)
                .FirstOrDefaultAsync();
        }

        permit ??= await _facilityPermitResolver.ResolveForDateAsync(facilityId, reportDate);

        if (permit == null)
        {
            return $"Permit versions exist, but none are active for {reportDate:MMM yyyy}. Update permit effective dates in System Administration > Facilities > Permit Versions.";
        }

        var hasNdmrRows = await _context.FacilityPermitTemplateParameters
            .AnyAsync(x => x.FacilityPermitId == permit.Id && (x.ReportTypes & PermitTemplateReportTypeEnum.Ndmr) != 0);

        if (!hasNdmrRows)
        {
            return $"Resolved permit {permit.PermitNumber} v{permit.PermitVersion} for {reportDate:MMM yyyy}, but it has no NDMR template parameters. Add PCS rows under Permit Versions.";
        }

        return "Permit template parameters are not available for this period.";
    }

    private async Task<List<GWMonitTemplateParameterViewModel>> BuildGwMonitTemplateInputsAsync(
        Guid facilityId,
        Guid? resolvedPermitId,
        DateTime sampleDate,
        Guid? gwMonitId = null)
    {
        var permitId = resolvedPermitId;
        if (!permitId.HasValue)
        {
            var permit = await _facilityPermitResolver.ResolveForDateAsync(facilityId, sampleDate);
            permitId = permit?.Id;
        }

        if (!permitId.HasValue) return new List<GWMonitTemplateParameterViewModel>();

        var templateRows = await _context.FacilityPermitTemplateParameters
            .Include(x => x.PcsParameterCatalog)
            .Where(x =>
                x.FacilityPermitId == permitId.Value &&
                ((x.ReportTypes & PermitTemplateReportTypeEnum.Gw59) != 0 ||
                 (x.ReportTypes & PermitTemplateReportTypeEnum.Gw59A) != 0))
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        var existingValues = gwMonitId.HasValue
            ? await _context.GWMonitTemplateValues.Where(x => x.GWMonitId == gwMonitId.Value).ToListAsync()
            : new List<GWMonitTemplateValue>();

        return templateRows.Select(row => new GWMonitTemplateParameterViewModel
        {
            FacilityPermitTemplateParameterId = row.Id,
            PcsCode = row.PcsParameterCatalog?.PcsCode ?? string.Empty,
            ParameterName = row.ParameterDisplayOverride ?? row.PcsParameterCatalog?.UserFriendlyName ?? row.PcsParameterCatalog?.OfficialParameterName ?? string.Empty,
            Units = row.UnitsOverride ?? row.PcsParameterCatalog?.AcceptedUnits ?? string.Empty,
            MeasurementFrequency = row.MeasurementFrequency.ToDisplayLabel(),
            SampleType = row.SampleType.ToString(),
            ScheduledMonthsCsv = row.ScheduledMonthsCsv,
            DailyMaximumLimit = row.DailyMaximumLimit,
            Notes = row.Notes,
            EnteredValue = existingValues.FirstOrDefault(v => v.FacilityPermitTemplateParameterId == row.Id)?.NumericValue,
            IsRequiredForSelectedMonth = IsTemplateRowApplicableForMonth(row, sampleDate.Month),
            RequirementMessage = IsTemplateRowApplicableForMonth(row, sampleDate.Month)
                ? $"Required for {sampleDate:MMM yyyy} based on permit schedule."
                : "Not required for this month by permit schedule."
        }).ToList();
    }

    private async Task<string?> BuildGwMonitTemplateStatusMessageAsync(
        Guid companyId,
        Guid facilityId,
        Guid? resolvedPermitId,
        DateTime sampleDate,
        int templateParameterCount)
    {
        if (templateParameterCount > 0) return null;
        if (facilityId == Guid.Empty) return "Select a facility to load permit template parameters.";

        var anyPermitsForFacility = await _context.FacilityPermits
            .AnyAsync(x => x.CompanyId == companyId && x.FacilityId == facilityId && !x.IsDeleted);

        if (!anyPermitsForFacility)
        {
            return "No permit versions are configured for this facility. Add a permit version first in System Administration > Facilities > Permit Versions.";
        }

        FacilityPermit? permit = null;
        if (resolvedPermitId.HasValue)
        {
            permit = await _context.FacilityPermits
                .Where(x => x.Id == resolvedPermitId.Value && !x.IsDeleted)
                .FirstOrDefaultAsync();
        }

        permit ??= await _facilityPermitResolver.ResolveForDateAsync(facilityId, sampleDate);

        if (permit == null)
        {
            return $"Permit versions exist, but none are active for {sampleDate:MMM yyyy}. Update permit effective dates in System Administration > Facilities > Permit Versions.";
        }

        var hasGwRows = await _context.FacilityPermitTemplateParameters
            .AnyAsync(x =>
                x.FacilityPermitId == permit.Id &&
                ((x.ReportTypes & PermitTemplateReportTypeEnum.Gw59) != 0 ||
                 (x.ReportTypes & PermitTemplateReportTypeEnum.Gw59A) != 0));

        if (!hasGwRows)
        {
            return $"Resolved permit {permit.PermitNumber} v{permit.PermitVersion} for {sampleDate:MMM yyyy}, but it has no groundwater template parameters. Add GW59/GW59A rows under Permit Versions.";
        }

        return "Permit template parameters are not available for this period.";
    }

    private static decimal? ComputeDailyLoadingFromVolume(decimal volumeGallons, decimal acres)
    {
        if (acres <= 0m)
        {
            return null;
        }

        return MonthlyApplicationCalculationHelper.ComputeDailyLoadingFromVolume(volumeGallons, acres);
    }

    #endregion
}


