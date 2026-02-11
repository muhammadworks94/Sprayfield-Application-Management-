using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SAM.Controllers.Base;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Authorization;
using SAM.Services.Interfaces;
using SAM.ViewModels.OperationalData;

namespace SAM.Controllers;

/// <summary>
/// Controller for Operational Data Entry module - managing daily and monthly logs.
/// </summary>
[Authorize(Policy = Policies.RequireTechnicianOrOperator)]
    public class OperationalDataController : BaseController
    {
        private readonly IOperatorLogService _operatorLogService;
        private readonly IIrrigateService _irrigateService;
        private readonly IWWCharService _wwCharService;
        private readonly IGWMonitService _gwMonitService;
        private readonly IFacilityService _facilityService;
        private readonly ISprayfieldService _sprayfieldService;
        private readonly IMonitoringWellService _monitoringWellService;
        private readonly ICompanyService _companyService;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public OperationalDataController(
            IOperatorLogService operatorLogService,
            IIrrigateService irrigateService,
            IWWCharService wwCharService,
            IGWMonitService gwMonitService,
            IFacilityService facilityService,
            ISprayfieldService sprayfieldService,
            IMonitoringWellService monitoringWellService,
            ICompanyService companyService,
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            UserManager<ApplicationUser> userManager,
            ILogger<OperationalDataController> logger)
            : base(userManager, logger)
        {
            _operatorLogService = operatorLogService;
            _irrigateService = irrigateService;
            _wwCharService = wwCharService;
            _gwMonitService = gwMonitService;
            _facilityService = facilityService;
            _sprayfieldService = sprayfieldService;
            _monitoringWellService = monitoringWellService;
            _companyService = companyService;
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
                WeatherConditions = viewModel.WeatherConditions,
                ArrivalTime = TimeSpan.Parse(viewModel.ArrivalTime),
                TimeOnSiteHours = viewModel.TimeOnSiteHours ?? 0,
                MaintenancePerformed = viewModel.MaintenancePerformed,
                EquipmentInspected = viewModel.EquipmentInspected,
                IssuesNoted = viewModel.IssuesNoted,
                CorrectiveActions = viewModel.CorrectiveActions,
                NextShiftNotes = viewModel.NextShiftNotes
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
            operatorLog.WeatherConditions = viewModel.WeatherConditions;
            operatorLog.ArrivalTime = TimeSpan.Parse(viewModel.ArrivalTime);
            operatorLog.TimeOnSiteHours = viewModel.TimeOnSiteHours ?? 0;
            operatorLog.MaintenancePerformed = viewModel.MaintenancePerformed;
            operatorLog.EquipmentInspected = viewModel.EquipmentInspected;
            operatorLog.IssuesNoted = viewModel.IssuesNoted;
            operatorLog.CorrectiveActions = viewModel.CorrectiveActions;
            operatorLog.NextShiftNotes = viewModel.NextShiftNotes;

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

    #region Irrigation Logs

    [HttpGet]
    public async Task<IActionResult> Irrigates(Guid? companyId = null, Guid? facilityId = null, Guid? sprayfieldId = null)
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

        var irrigates = await _irrigateService.GetAllAsync(companyId, facilityId, sprayfieldId);

        var viewModels = irrigates.Select(i => new IrrigateViewModel
        {
            Id = i.Id,
            CompanyId = i.CompanyId,
            CompanyName = i.Company?.Name,
            FacilityId = i.FacilityId,
            FacilityName = i.Facility?.Name,
            SprayfieldId = i.SprayfieldId,
            SprayfieldName = i.Sprayfield?.FieldId,
            IrrigationDate = i.IrrigationDate,
            StartTime = i.StartTime.ToString(@"hh\:mm"),
            EndTime = i.EndTime.ToString(@"hh\:mm"),
            DurationHours = i.DurationHours,
            FlowRateGpm = i.FlowRateGpm,
            TotalVolumeGallons = i.TotalVolumeGallons,
            ApplicationRateInches = i.ApplicationRateInches,
            TemperatureF = i.TemperatureF,
            PrecipitationIn = i.PrecipitationIn,
            WeatherConditions = i.WeatherConditions,
            Comments = i.Comments,
            ModifiedBy = i.ModifiedBy
        });

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        ViewBag.Facilities = await GetFacilitySelectListAsync(companyId);
        ViewBag.Sprayfields = await GetSprayfieldSelectListAsync(companyId, facilityId);
        ViewBag.SelectedCompanyId = companyId;
        ViewBag.SelectedFacilityId = facilityId;
        ViewBag.SelectedSprayfieldId = sprayfieldId;

        return View(viewModels);
    }

    [HttpGet]
    public async Task<IActionResult> IrrigateDetails(Guid id)
    {
        var irrigate = await _irrigateService.GetByIdAsync(id);
        if (irrigate == null)
            return NotFound();

        await EnsureCompanyAccessAsync(irrigate.CompanyId);

        var viewModel = new IrrigateViewModel
        {
            Id = irrigate.Id,
            CompanyId = irrigate.CompanyId,
            CompanyName = irrigate.Company?.Name,
            FacilityId = irrigate.FacilityId,
            FacilityName = irrigate.Facility?.Name,
            SprayfieldId = irrigate.SprayfieldId,
            SprayfieldName = irrigate.Sprayfield?.FieldId,
            IrrigationDate = irrigate.IrrigationDate,
            StartTime = irrigate.StartTime.ToString(@"hh\:mm"),
            EndTime = irrigate.EndTime.ToString(@"hh\:mm"),
            DurationHours = irrigate.DurationHours,
            FlowRateGpm = irrigate.FlowRateGpm,
            TotalVolumeGallons = irrigate.TotalVolumeGallons,
            ApplicationRateInches = irrigate.ApplicationRateInches,
            TemperatureF = irrigate.TemperatureF,
            PrecipitationIn = irrigate.PrecipitationIn,
            WeatherConditions = irrigate.WeatherConditions,
            Comments = irrigate.Comments,
            ModifiedBy = irrigate.ModifiedBy
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> IrrigateCreate(Guid? companyId = null, Guid? facilityId = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // For non-global users, default company from their context when not provided
        // Set company ID if not provided (respects session selection for admins)
        if (!companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        // For global admins (no company assigned) but with a facility selected,
        // derive the company from the chosen facility so downstream filters work
        if ((companyId == null || companyId == Guid.Empty) && facilityId.HasValue)
        {
            var facility = await _facilityService.GetByIdAsync(facilityId.Value);
            if (facility != null)
            {
                companyId = facility.CompanyId;
            }
        }

        // Only enforce company access when we have a real company id
        if (companyId.HasValue && companyId != Guid.Empty)
        {
            await EnsureCompanyAccessAsync(companyId.Value);
        }

        var viewModel = new IrrigateCreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty,
            FacilityId = facilityId ?? Guid.Empty
        };

        // Populate dropdowns
        ViewBag.Facilities = await GetFacilitySelectListAsync(companyId);
        ViewBag.Sprayfields = await GetSprayfieldSelectListAsync(companyId, facilityId);
        ViewBag.Companies = await GetCompanySelectListAsync();

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> GetSprayfieldsForFacility(Guid facilityId)
    {
        if (facilityId == Guid.Empty)
            return BadRequest("Facility is required.");

        var facility = await _facilityService.GetByIdAsync(facilityId);
        if (facility == null)
            return NotFound("Facility not found.");

        // Enforce access using the facility's company
        await EnsureCompanyAccessAsync(facility.CompanyId);

        // Get sprayfields for this facility only
        var sprayfields = await _sprayfieldService.GetAllAsync(facility.CompanyId);
        var filtered = sprayfields
            .Where(s => s.FacilityId == facilityId)
            .Select(s => new
            {
                id = s.Id,
                name = s.FieldId
            })
            .ToList();

        return Json(new
        {
            companyId = facility.CompanyId,
            sprayfields = filtered
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IrrigateCreate(IrrigateCreateViewModel viewModel)
    {
        // For global admins or cases where CompanyId was not bound,
        // derive the company from the selected facility so access checks succeed.
        if ((viewModel.CompanyId == Guid.Empty || viewModel.CompanyId == default) &&
            viewModel.FacilityId != Guid.Empty)
        {
            var facility = await _facilityService.GetByIdAsync(viewModel.FacilityId);
            if (facility != null)
            {
                viewModel.CompanyId = facility.CompanyId;
            }
        }

        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Sprayfields = await GetSprayfieldSelectListAsync(viewModel.CompanyId, viewModel.FacilityId);
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }

        try
        {
            var currentUser = await GetCurrentUserAsync();
            var modifiedBy = currentUser == null
                ? null
                : (string.IsNullOrWhiteSpace(currentUser.FullName) ? currentUser.UserName : currentUser.FullName);

            var irrigate = new Irrigate
            {
                CompanyId = viewModel.CompanyId,
                FacilityId = viewModel.FacilityId,
                SprayfieldId = viewModel.SprayfieldId,
                IrrigationDate = viewModel.IrrigationDate,
                StartTime = TimeSpan.Parse(viewModel.StartTime),
                EndTime = TimeSpan.Parse(viewModel.EndTime),
                DurationHours = viewModel.DurationHours ?? 0,
                FlowRateGpm = viewModel.FlowRateGpm ?? 0,
                TotalVolumeGallons = viewModel.TotalVolumeGallons ?? 0,
                ApplicationRateInches = viewModel.ApplicationRateInches ?? 0,
                TemperatureF = viewModel.TemperatureF,
                PrecipitationIn = viewModel.PrecipitationIn,
                WeatherConditions = viewModel.WeatherConditions,
                Comments = viewModel.Comments,
                ModifiedBy = modifiedBy
            };

            await _irrigateService.CreateAsync(irrigate);
            TempData["SuccessMessage"] = "Irrigation log created successfully.";
            var isGlobalAdmin = await IsGlobalAdminAsync();
            if (isGlobalAdmin)
            {
                return RedirectToAction(nameof(Irrigates));

            }
            else
            {
                return RedirectToAction(nameof(Irrigates), new { facilityId = irrigate.FacilityId });
               
            }
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Sprayfields = await GetSprayfieldSelectListAsync(viewModel.CompanyId, viewModel.FacilityId);
            return View(viewModel);
        }
    }

    [HttpGet]
    public async Task<IActionResult> IrrigateEdit(Guid id)
    {
        var irrigate = await _irrigateService.GetByIdAsync(id);
        if (irrigate == null)
            return NotFound();

        await EnsureCompanyAccessAsync(irrigate.CompanyId);

        var viewModel = new IrrigateEditViewModel
        {
            Id = irrigate.Id,
            CompanyId = irrigate.CompanyId,
            FacilityId = irrigate.FacilityId,
            SprayfieldId = irrigate.SprayfieldId,
            IrrigationDate = irrigate.IrrigationDate,
            StartTime = irrigate.StartTime.ToString(@"hh\:mm"),
            EndTime = irrigate.EndTime.ToString(@"hh\:mm"),
            DurationHours = irrigate.DurationHours,
            FlowRateGpm = irrigate.FlowRateGpm,
            TotalVolumeGallons = irrigate.TotalVolumeGallons,
            ApplicationRateInches = irrigate.ApplicationRateInches,
            TemperatureF = irrigate.TemperatureF,
            PrecipitationIn = irrigate.PrecipitationIn,
            WeatherConditions = irrigate.WeatherConditions,
            Comments = irrigate.Comments
        };

        ViewBag.Facilities = await GetFacilitySelectListAsync(irrigate.CompanyId);
        ViewBag.Sprayfields = await GetSprayfieldSelectListAsync(irrigate.CompanyId, irrigate.FacilityId);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IrrigateEdit(IrrigateEditViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Sprayfields = await GetSprayfieldSelectListAsync(viewModel.CompanyId, viewModel.FacilityId);
            return View(viewModel);
        }

        try
        {
            var irrigate = await _irrigateService.GetByIdAsync(viewModel.Id);
            if (irrigate == null)
                return NotFound();

            var currentUser = await GetCurrentUserAsync();
            var modifiedBy = currentUser == null
                ? null
                : (string.IsNullOrWhiteSpace(currentUser.FullName) ? currentUser.UserName : currentUser.FullName);

            irrigate.IrrigationDate = viewModel.IrrigationDate;
            irrigate.StartTime = TimeSpan.Parse(viewModel.StartTime);
            irrigate.EndTime = TimeSpan.Parse(viewModel.EndTime);
            irrigate.DurationHours = viewModel.DurationHours ?? 0;
            irrigate.FlowRateGpm = viewModel.FlowRateGpm ?? 0;
            irrigate.TotalVolumeGallons = viewModel.TotalVolumeGallons ?? 0;
            irrigate.ApplicationRateInches = viewModel.ApplicationRateInches ?? 0;
            irrigate.TemperatureF = viewModel.TemperatureF;
            irrigate.PrecipitationIn = viewModel.PrecipitationIn;
            irrigate.WeatherConditions = viewModel.WeatherConditions;
            irrigate.Comments = viewModel.Comments;
            irrigate.ModifiedBy = modifiedBy;

            await _irrigateService.UpdateAsync(irrigate);
            TempData["SuccessMessage"] = "Irrigation log updated successfully.";
            return RedirectToAction(nameof(Irrigates), new { facilityId = irrigate.FacilityId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Sprayfields = await GetSprayfieldSelectListAsync(viewModel.CompanyId, viewModel.FacilityId);
            return View(viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IrrigateDelete(Guid id)
    {
        try
        {
            var irrigate = await _irrigateService.GetByIdAsync(id);
            if (irrigate != null)
            {
                await EnsureCompanyAccessAsync(irrigate.CompanyId);
            }

            await _irrigateService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Irrigation log deleted successfully.";
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "Irrigation log not found.";
        }

        return RedirectToAction(nameof(Irrigates));
    }

    #endregion

    #region Wastewater Characteristics (WWChar)

    [HttpGet]
    public async Task<IActionResult> WWChars(Guid? companyId = null, Guid? facilityId = null)
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
            AnalyzedBy = w.AnalyzedBy
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
            AnalyzedBy = wwChar.AnalyzedBy
        };

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireTechnician)]
    public async Task<IActionResult> WWCharCreate(Guid? companyId = null, Guid? facilityId = null)
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

        // Initialize daily arrays with 31 empty entries
        InitializeDailyArrays(viewModel);

        ViewBag.Facilities = await GetFacilitySelectListAsync(companyId);
        ViewBag.Months = GetMonthSelectList();
        ViewBag.ORCOnSiteOptions = GetORCOnSiteSelectList();

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

        if (!ModelState.IsValid)
        {
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Months = GetMonthSelectList();
            ViewBag.ORCOnSiteOptions = GetORCOnSiteSelectList();
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
                AnalyzedBy = viewModel.AnalyzedBy
            };

            await _wwCharService.CreateAsync(wwChar);
            TempData["SuccessMessage"] = $"Wastewater characteristics record created for {wwChar.Month} {wwChar.Year}.";
            return RedirectToAction(nameof(WWChars), new { facilityId = wwChar.FacilityId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Months = GetMonthSelectList();
            ViewBag.ORCOnSiteOptions = GetORCOnSiteSelectList();
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
            AnalyzedBy = wwChar.AnalyzedBy
        };

        // Ensure arrays are initialized with 31 entries
        EnsureDailyArraysInitialized(viewModel);

        ViewBag.Facilities = await GetFacilitySelectListAsync(wwChar.CompanyId);
        ViewBag.Months = GetMonthSelectList();
        ViewBag.ORCOnSiteOptions = GetORCOnSiteSelectList();

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireTechnician)]
    public async Task<IActionResult> WWCharEdit(WWCharEditViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        // Ensure arrays are initialized
        EnsureDailyArraysInitialized(viewModel);

        if (!ModelState.IsValid)
        {
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Months = GetMonthSelectList();
            ViewBag.ORCOnSiteOptions = GetORCOnSiteSelectList();
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

            await _wwCharService.UpdateAsync(wwChar);
            TempData["SuccessMessage"] = $"Wastewater characteristics record updated for {wwChar.Month} {wwChar.Year}.";
            return RedirectToAction(nameof(WWChars), new { facilityId = wwChar.FacilityId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Months = GetMonthSelectList();
            ViewBag.ORCOnSiteOptions = GetORCOnSiteSelectList();
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
            DrawText(reportModel.FacilityName, 120, 74);                  // Facility Name
            DrawText(reportModel.PermitNumber, 630, 60);                 // Permit Number
            DrawText(reportModel.Permittee, 170, 90);                    // Permit Name
            DrawText($"{reportModel.Address}", 120, 106);                 // Address line
            DrawText(reportModel.City, 80, 140);
            DrawText(reportModel.ZipCode, 280, 140);
            DrawText( reportModel.State , 230, 140);
            DrawText(reportModel.County, 410, 120);                      // County

            // Contact / phone – using facility phone
            DrawText(reportModel.FacilityPhone, 410, 152);

            // Sampling information / well details
            DrawText(reportModel.WellId, 210, 204);                       // WELL ID NUMBER
            DrawText(reportModel.SampleDate.ToString("MM/dd/yyyy"), 460, 204); // Date sample collected

            DrawText(reportModel.WellDepthFeet?.ToString("F2"), 110, 220); // Well Depth
            DrawText(reportModel.DiameterInches?.ToString("F2"), 457, 220); // Well Diameter

            DrawText(reportModel.SampleDepth?.ToString("F2"), 90, 220);  // Sample depth
            DrawText(reportModel.WaterLevel?.ToString("F2"), 160, 237);  // Depth to water

            DrawText(reportModel.GallonsPumped?.ToString("F2"), 260, 266); // Volume pumped

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
            DrawText(reportModel.LabName, 450, 308);                      // Laboratory Name
            DrawText(reportModel.LabCertificationNumber, 740, 308);      // Certification No.

            // Core parameters from GWMonit
            DrawText(reportModel.TDS?.ToString("F2"), 160, 400);          // Dissolved Solids: Total
            DrawText(reportModel.TOC?.ToString("F2"), 165, 430);          // TOC
            DrawText(reportModel.Chloride?.ToString("F2"), 165, 446);     // Chloride

            DrawText(reportModel.NH3N?.ToString("F2"), 165, 538);         // Total Ammonia
            DrawText(reportModel.TKN?.ToString("F2"), 165, 571);          // TKN as N

            DrawText(reportModel.NO3N?.ToString("F2"), 440, 352);        // Nitrate (NO3) as N

            DrawText(reportModel.Calcium?.ToString("F2"), 440, 430);     // Ca
            DrawText(reportModel.Magnesium?.ToString("F2"), 440, 538);   // Mg

            DrawText(reportModel.FecalColiform?.ToString("F0"), 165, 352); // Coliform MF Fecal
            DrawText(reportModel.TotalColiform?.ToString("F0"), 165, 369); // Coliform MF Total

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
            FacilityId = facilityId ?? Guid.Empty
        };

        ViewBag.Facilities = await GetFacilitySelectListAsync(companyId);
        ViewBag.MonitoringWells = await GetMonitoringWellSelectListAsync(companyId, facilityId);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireTechnician)]
    public async Task<IActionResult> GWMonitCreate(GWMonitCreateViewModel viewModel)
    {
        // Derive company from the selected facility to support global admins
        var facility = await _facilityService.GetByIdAsync(viewModel.FacilityId);
        if (facility == null)
        {
            ModelState.AddModelError("FacilityId", "Selected facility was not found.");
        }

        if (!ModelState.IsValid)
        {
            var companyIdForLists = facility?.CompanyId != Guid.Empty ? facility?.CompanyId : viewModel.CompanyId;
            ViewBag.Facilities = await GetFacilitySelectListAsync(companyIdForLists);
            ViewBag.MonitoringWells = await GetMonitoringWellSelectListAsync(companyIdForLists, viewModel.FacilityId);
            return View(viewModel);
        }

        await EnsureCompanyAccessAsync(facility!.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.MonitoringWells = await GetMonitoringWellSelectListAsync(viewModel.CompanyId, viewModel.FacilityId);
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
                PH = viewModel.PH,
                GallonsPumped = viewModel.GallonsPumped,
                Odor = viewModel.Odor,
                Appearance = viewModel.Appearance,
                Conductivity = viewModel.Conductivity,
                TDS = viewModel.TDS,
                Turbidity = viewModel.Turbidity,
                TSS = viewModel.TSS,
                NH3N = viewModel.NH3N,
                NO3N = viewModel.NO3N,
                TKN = viewModel.TKN,
                TOC = viewModel.TOC,
                Chloride = viewModel.Chloride,
                Calcium = viewModel.Calcium,
                Magnesium = viewModel.Magnesium,
                MetalsSamplesCollectedUnfiltered = viewModel.MetalsSamplesCollectedUnfiltered,
                MetalSamplesFieldAcidified = viewModel.MetalSamplesFieldAcidified,
                FecalColiform = viewModel.FecalColiform,
                TotalColiform = viewModel.TotalColiform,
                VOCReportAttached = viewModel.VOCReportAttached,
                VOCMethodNumber = viewModel.VOCMethodNumber,
                LabCertification = viewModel.LabCertification,
                CollectedBy = viewModel.CollectedBy,
                AnalyzedBy = viewModel.AnalyzedBy,
                Comments = viewModel.Comments
            };

            await _gwMonitService.CreateAsync(gwMonit);
            TempData["SuccessMessage"] = "Groundwater monitoring record created successfully.";
            return RedirectToAction(nameof(GWMonits), new { facilityId = gwMonit.FacilityId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
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

        if (!ModelState.IsValid)
        {
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
            gwMonit.PH = viewModel.PH;
            gwMonit.GallonsPumped = viewModel.GallonsPumped;
            gwMonit.Odor = viewModel.Odor;
            gwMonit.Appearance = viewModel.Appearance;
            gwMonit.Conductivity = viewModel.Conductivity;
            gwMonit.TDS = viewModel.TDS;
            gwMonit.Turbidity = viewModel.Turbidity;
            gwMonit.TSS = viewModel.TSS;
            gwMonit.NH3N = viewModel.NH3N;
            gwMonit.NO3N = viewModel.NO3N;
            gwMonit.TKN = viewModel.TKN;
            gwMonit.TOC = viewModel.TOC;
            gwMonit.Chloride = viewModel.Chloride;
            gwMonit.Calcium = viewModel.Calcium;
            gwMonit.Magnesium = viewModel.Magnesium;
            gwMonit.MetalsSamplesCollectedUnfiltered = viewModel.MetalsSamplesCollectedUnfiltered;
            gwMonit.MetalSamplesFieldAcidified = viewModel.MetalSamplesFieldAcidified;
            gwMonit.FecalColiform = viewModel.FecalColiform;
            gwMonit.TotalColiform = viewModel.TotalColiform;
            gwMonit.VOCReportAttached = viewModel.VOCReportAttached;
            gwMonit.VOCMethodNumber = viewModel.VOCMethodNumber;
            gwMonit.LabCertification = viewModel.LabCertification;
            gwMonit.CollectedBy = viewModel.CollectedBy;
            gwMonit.AnalyzedBy = viewModel.AnalyzedBy;
            gwMonit.Comments = viewModel.Comments;

            await _gwMonitService.UpdateAsync(gwMonit);
            TempData["SuccessMessage"] = "Groundwater monitoring record updated successfully.";
            return RedirectToAction(nameof(GWMonits), new { facilityId = gwMonit.FacilityId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
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

    private async Task<SelectList> GetSprayfieldSelectListAsync(Guid? companyId = null, Guid? facilityId = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // Use effective company ID if no companyId specified (respects session selection for admins)
        if (!companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        var sprayfields = await _sprayfieldService.GetAllAsync(companyId);
        
        if (facilityId.HasValue)
        {
            sprayfields = sprayfields.Where(s => s.FacilityId == facilityId.Value);
        }

        var items = sprayfields.Select(s => new SelectListItem
        {
            Value = s.Id.ToString(),
            Text = s.FieldId
        }).ToList();

        return new SelectList(items, "Value", "Text");
    }

    private async Task<SelectList> GetCompanySelectListAsync()
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();
        var companies = await _companyService.GetAllAsync();

        // Filter by effective company ID if session has a selection (for admins) or user has a company
        if (effectiveCompanyId.HasValue)
        {
            companies = companies.Where(c => c.Id == effectiveCompanyId.Value);
        }

        return new SelectList(companies, "Id", "Name");
    }

    private async Task<SelectList> GetMonitoringWellSelectListAsync(Guid? companyId = null, Guid? facilityId = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // Use effective company ID if no companyId specified (respects session selection for admins)
        if (!companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        var monitoringWells = await _monitoringWellService.GetAllAsync(companyId);
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

    #endregion
}

