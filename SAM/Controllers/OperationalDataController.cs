using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SAM.Controllers.Base;
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
[Authorize(Policy = Policies.RequireOperator)]
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
        }

    #region Operator Logs

    [HttpGet]
    public async Task<IActionResult> OperatorLogs(Guid? companyId = null, Guid? facilityId = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        if (!isGlobalAdmin && !companyId.HasValue && effectiveCompanyId.HasValue)
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
            Shift = l.Shift,
            WeatherConditions = l.WeatherConditions,
            SystemStatus = l.SystemStatus,
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
            Shift = log.Shift,
            WeatherConditions = log.WeatherConditions,
            SystemStatus = log.SystemStatus,
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

        if (!companyId.HasValue && !isGlobalAdmin && effectiveCompanyId.HasValue)
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
        ViewBag.Shifts = GetShiftSelectList();
        ViewBag.SystemStatuses = GetSystemStatusSelectList();

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OperatorLogCreate(OperatorLogCreateViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Shifts = GetShiftSelectList();
            ViewBag.SystemStatuses = GetSystemStatusSelectList();
            return View(viewModel);
        }

        try
        {
            var operatorLog = new OperatorLog
            {
                CompanyId = viewModel.CompanyId,
                FacilityId = viewModel.FacilityId,
                LogDate = viewModel.LogDate,
                OperatorName = viewModel.OperatorName,
                Shift = viewModel.Shift,
                WeatherConditions = viewModel.WeatherConditions,
                SystemStatus = viewModel.SystemStatus,
                MaintenancePerformed = viewModel.MaintenancePerformed,
                EquipmentInspected = viewModel.EquipmentInspected,
                IssuesNoted = viewModel.IssuesNoted,
                CorrectiveActions = viewModel.CorrectiveActions,
                NextShiftNotes = viewModel.NextShiftNotes
            };

            await _operatorLogService.CreateAsync(operatorLog);
            TempData["SuccessMessage"] = "Operator log created successfully.";
            return RedirectToAction(nameof(OperatorLogs), new { companyId = operatorLog.CompanyId, facilityId = operatorLog.FacilityId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Shifts = GetShiftSelectList();
            ViewBag.SystemStatuses = GetSystemStatusSelectList();
            return View(viewModel);
        }
    }

    [HttpGet]
    public async Task<IActionResult> OperatorLogEdit(Guid id)
    {
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
            OperatorName = log.OperatorName,
            Shift = log.Shift,
            WeatherConditions = log.WeatherConditions,
            SystemStatus = log.SystemStatus,
            MaintenancePerformed = log.MaintenancePerformed,
            EquipmentInspected = log.EquipmentInspected,
            IssuesNoted = log.IssuesNoted,
            CorrectiveActions = log.CorrectiveActions,
            NextShiftNotes = log.NextShiftNotes
        };

        ViewBag.Facilities = await GetFacilitySelectListAsync(log.CompanyId);
        ViewBag.Shifts = GetShiftSelectList();
        ViewBag.SystemStatuses = GetSystemStatusSelectList();

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
            ViewBag.Shifts = GetShiftSelectList();
            ViewBag.SystemStatuses = GetSystemStatusSelectList();
            return View(viewModel);
        }

        try
        {
            var operatorLog = await _operatorLogService.GetByIdAsync(viewModel.Id);
            if (operatorLog == null)
                return NotFound();

            operatorLog.LogDate = viewModel.LogDate;
            operatorLog.OperatorName = viewModel.OperatorName;
            operatorLog.Shift = viewModel.Shift;
            operatorLog.WeatherConditions = viewModel.WeatherConditions;
            operatorLog.SystemStatus = viewModel.SystemStatus;
            operatorLog.MaintenancePerformed = viewModel.MaintenancePerformed;
            operatorLog.EquipmentInspected = viewModel.EquipmentInspected;
            operatorLog.IssuesNoted = viewModel.IssuesNoted;
            operatorLog.CorrectiveActions = viewModel.CorrectiveActions;
            operatorLog.NextShiftNotes = viewModel.NextShiftNotes;

            await _operatorLogService.UpdateAsync(operatorLog);
            TempData["SuccessMessage"] = "Operator log updated successfully.";
            return RedirectToAction(nameof(OperatorLogs), new { companyId = operatorLog.CompanyId, facilityId = operatorLog.FacilityId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Facilities = await GetFacilitySelectListAsync(viewModel.CompanyId);
            ViewBag.Shifts = GetShiftSelectList();
            ViewBag.SystemStatuses = GetSystemStatusSelectList();
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

        if (!isGlobalAdmin && !companyId.HasValue && effectiveCompanyId.HasValue)
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
            WindSpeed = i.WindSpeed,
            WindDirection = i.WindDirection,
            WeatherConditions = i.WeatherConditions,
            Operator = i.Operator,
            Comments = i.Comments
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
            WindSpeed = irrigate.WindSpeed,
            WindDirection = irrigate.WindDirection,
            WeatherConditions = irrigate.WeatherConditions,
            Operator = irrigate.Operator,
            Comments = irrigate.Comments
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> IrrigateCreate(Guid? companyId = null, Guid? facilityId = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // For non-global users, default company from their context when not provided
        if (!companyId.HasValue && !isGlobalAdmin && effectiveCompanyId.HasValue)
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
                WindSpeed = viewModel.WindSpeed,
                WindDirection = viewModel.WindDirection,
                WeatherConditions = viewModel.WeatherConditions,
                Operator = viewModel.Operator,
                Comments = viewModel.Comments
            };

            await _irrigateService.CreateAsync(irrigate);
            TempData["SuccessMessage"] = "Irrigation log created successfully.";
            return RedirectToAction(nameof(Irrigates), new { companyId = irrigate.CompanyId, facilityId = irrigate.FacilityId });
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
            WindSpeed = irrigate.WindSpeed,
            WindDirection = irrigate.WindDirection,
            WeatherConditions = irrigate.WeatherConditions,
            Operator = irrigate.Operator,
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

            irrigate.IrrigationDate = viewModel.IrrigationDate;
            irrigate.StartTime = TimeSpan.Parse(viewModel.StartTime);
            irrigate.EndTime = TimeSpan.Parse(viewModel.EndTime);
            irrigate.DurationHours = viewModel.DurationHours ?? 0;
            irrigate.FlowRateGpm = viewModel.FlowRateGpm ?? 0;
            irrigate.TotalVolumeGallons = viewModel.TotalVolumeGallons ?? 0;
            irrigate.ApplicationRateInches = viewModel.ApplicationRateInches ?? 0;
            irrigate.WindSpeed = viewModel.WindSpeed;
            irrigate.WindDirection = viewModel.WindDirection;
            irrigate.WeatherConditions = viewModel.WeatherConditions;
            irrigate.Operator = viewModel.Operator;
            irrigate.Comments = viewModel.Comments;

            await _irrigateService.UpdateAsync(irrigate);
            TempData["SuccessMessage"] = "Irrigation log updated successfully.";
            return RedirectToAction(nameof(Irrigates), new { companyId = irrigate.CompanyId, facilityId = irrigate.FacilityId });
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

        if (!isGlobalAdmin && !companyId.HasValue && effectiveCompanyId.HasValue)
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
            TotalColiformDaily = w.TotalColiformDaily,
            ChlorideDaily = w.ChlorideDaily,
            TDSDaily = w.TDSDaily,
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
            TotalColiformDaily = wwChar.TotalColiformDaily,
            ChlorideDaily = wwChar.ChlorideDaily,
            TDSDaily = wwChar.TDSDaily,
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

        if (!companyId.HasValue && !isGlobalAdmin && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
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
                TotalColiformDaily = viewModel.TotalColiformDaily,
                ChlorideDaily = viewModel.ChlorideDaily,
                TDSDaily = viewModel.TDSDaily,
                CompositeTime = viewModel.CompositeTime,
                ORCOnSite = viewModel.ORCOnSite,
                LagoonFreeboard = viewModel.LagoonFreeboard,
                LabCertification = viewModel.LabCertification,
                CollectedBy = viewModel.CollectedBy,
                AnalyzedBy = viewModel.AnalyzedBy
            };

            await _wwCharService.CreateAsync(wwChar);
            TempData["SuccessMessage"] = $"Wastewater characteristics record created for {wwChar.Month} {wwChar.Year}.";
            return RedirectToAction(nameof(WWChars), new { companyId = wwChar.CompanyId, facilityId = wwChar.FacilityId });
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
            TotalColiformDaily = wwChar.TotalColiformDaily,
            ChlorideDaily = wwChar.ChlorideDaily,
            TDSDaily = wwChar.TDSDaily,
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
            wwChar.TotalColiformDaily = viewModel.TotalColiformDaily;
            wwChar.ChlorideDaily = viewModel.ChlorideDaily;
            wwChar.TDSDaily = viewModel.TDSDaily;
            wwChar.CompositeTime = viewModel.CompositeTime;
            wwChar.ORCOnSite = viewModel.ORCOnSite;
            wwChar.LagoonFreeboard = viewModel.LagoonFreeboard;
            wwChar.LabCertification = viewModel.LabCertification;
            wwChar.CollectedBy = viewModel.CollectedBy;
            wwChar.AnalyzedBy = viewModel.AnalyzedBy;

            await _wwCharService.UpdateAsync(wwChar);
            TempData["SuccessMessage"] = $"Wastewater characteristics record updated for {wwChar.Month} {wwChar.Year}.";
            return RedirectToAction(nameof(WWChars), new { companyId = wwChar.CompanyId, facilityId = wwChar.FacilityId });
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

        if (!isGlobalAdmin && !companyId.HasValue && effectiveCompanyId.HasValue)
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
            Conductivity = g.Conductivity,
            TDS = g.TDS,
            Turbidity = g.Turbidity,
            BOD5 = g.BOD5,
            COD = g.COD,
            TSS = g.TSS,
            NH3N = g.NH3N,
            NO3N = g.NO3N,
            TKN = g.TKN,
            TotalPhosphorus = g.TotalPhosphorus,
            Chloride = g.Chloride,
            FecalColiform = g.FecalColiform,
            TotalColiform = g.TotalColiform,
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
            Conductivity = gwMonit.Conductivity,
            TDS = gwMonit.TDS,
            Turbidity = gwMonit.Turbidity,
            BOD5 = gwMonit.BOD5,
            COD = gwMonit.COD,
            TSS = gwMonit.TSS,
            NH3N = gwMonit.NH3N,
            NO3N = gwMonit.NO3N,
            TKN = gwMonit.TKN,
            TotalPhosphorus = gwMonit.TotalPhosphorus,
            Chloride = gwMonit.Chloride,
            FecalColiform = gwMonit.FecalColiform,
            TotalColiform = gwMonit.TotalColiform,
            LabCertification = gwMonit.LabCertification,
            CollectedBy = gwMonit.CollectedBy,
            AnalyzedBy = gwMonit.AnalyzedBy,
            Comments = gwMonit.Comments
        };

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireTechnician)]
    public async Task<IActionResult> GWMonitCreate(Guid? companyId = null, Guid? facilityId = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // If a non-global user, default to their company when no company is specified
        if (!companyId.HasValue && !isGlobalAdmin && effectiveCompanyId.HasValue)
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
                Conductivity = viewModel.Conductivity,
                TDS = viewModel.TDS,
                Turbidity = viewModel.Turbidity,
                BOD5 = viewModel.BOD5,
                COD = viewModel.COD,
                TSS = viewModel.TSS,
                NH3N = viewModel.NH3N,
                NO3N = viewModel.NO3N,
                TKN = viewModel.TKN,
                TotalPhosphorus = viewModel.TotalPhosphorus,
                Chloride = viewModel.Chloride,
                FecalColiform = viewModel.FecalColiform,
                TotalColiform = viewModel.TotalColiform,
                LabCertification = viewModel.LabCertification,
                CollectedBy = viewModel.CollectedBy,
                AnalyzedBy = viewModel.AnalyzedBy,
                Comments = viewModel.Comments
            };

            await _gwMonitService.CreateAsync(gwMonit);
            TempData["SuccessMessage"] = "Groundwater monitoring record created successfully.";
            return RedirectToAction(nameof(GWMonits), new { companyId = gwMonit.CompanyId, facilityId = gwMonit.FacilityId });
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
            Conductivity = gwMonit.Conductivity,
            TDS = gwMonit.TDS,
            Turbidity = gwMonit.Turbidity,
            BOD5 = gwMonit.BOD5,
            COD = gwMonit.COD,
            TSS = gwMonit.TSS,
            NH3N = gwMonit.NH3N,
            NO3N = gwMonit.NO3N,
            TKN = gwMonit.TKN,
            TotalPhosphorus = gwMonit.TotalPhosphorus,
            Chloride = gwMonit.Chloride,
            FecalColiform = gwMonit.FecalColiform,
            TotalColiform = gwMonit.TotalColiform,
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
            gwMonit.Conductivity = viewModel.Conductivity;
            gwMonit.TDS = viewModel.TDS;
            gwMonit.Turbidity = viewModel.Turbidity;
            gwMonit.BOD5 = viewModel.BOD5;
            gwMonit.COD = viewModel.COD;
            gwMonit.TSS = viewModel.TSS;
            gwMonit.NH3N = viewModel.NH3N;
            gwMonit.NO3N = viewModel.NO3N;
            gwMonit.TKN = viewModel.TKN;
            gwMonit.TotalPhosphorus = viewModel.TotalPhosphorus;
            gwMonit.Chloride = viewModel.Chloride;
            gwMonit.FecalColiform = viewModel.FecalColiform;
            gwMonit.TotalColiform = viewModel.TotalColiform;
            gwMonit.LabCertification = viewModel.LabCertification;
            gwMonit.CollectedBy = viewModel.CollectedBy;
            gwMonit.AnalyzedBy = viewModel.AnalyzedBy;
            gwMonit.Comments = viewModel.Comments;

            await _gwMonitService.UpdateAsync(gwMonit);
            TempData["SuccessMessage"] = "Groundwater monitoring record updated successfully.";
            return RedirectToAction(nameof(GWMonits), new { companyId = gwMonit.CompanyId, facilityId = gwMonit.FacilityId });
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

        if (!isGlobalAdmin && !companyId.HasValue && effectiveCompanyId.HasValue)
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

        if (!isGlobalAdmin && !companyId.HasValue && effectiveCompanyId.HasValue)
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

        if (!isGlobalAdmin && effectiveCompanyId.HasValue)
        {
            companies = companies.Where(c => c.Id == effectiveCompanyId.Value);
        }

        return new SelectList(companies, "Id", "Name");
    }

    private async Task<SelectList> GetMonitoringWellSelectListAsync(Guid? companyId = null, Guid? facilityId = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        if (!isGlobalAdmin && !companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        var monitoringWells = await _monitoringWellService.GetAllAsync(companyId);
        return new SelectList(monitoringWells, "Id", "WellId");
    }

    private SelectList GetShiftSelectList()
    {
        return new SelectList(Enum.GetValues(typeof(ShiftEnum)).Cast<ShiftEnum>()
            .Select(e => new SelectListItem
            {
                Value = e.ToString(),
                Text = e.ToString()
            }), "Value", "Text");
    }

    private SelectList GetSystemStatusSelectList()
    {
        return new SelectList(Enum.GetValues(typeof(SystemStatusEnum)).Cast<SystemStatusEnum>()
            .Select(e => new SelectListItem
            {
                Value = e.ToString(),
                Text = e.ToString()
            }), "Value", "Text");
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
        EnsureArraySize(viewModel.TotalColiformDaily, 31);
        EnsureArraySize(viewModel.ChlorideDaily, 31);
        EnsureArraySize(viewModel.TDSDaily, 31);
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
        EnsureArraySize(viewModel.TotalColiformDaily, 31);
        EnsureArraySize(viewModel.ChlorideDaily, 31);
        EnsureArraySize(viewModel.TDSDaily, 31);
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

