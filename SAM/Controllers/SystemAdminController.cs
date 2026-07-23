using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using SAM.Controllers.Base;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Authorization;
using SAM.Services.Interfaces;
using SAM.Utilities;
using SAM.ViewModels.Common;
using SAM.ViewModels.SystemAdmin;

namespace SAM.Controllers;

/// <summary>
/// Controller for System Administration module - managing reference data.
/// </summary>

public partial class SystemAdminController : BaseController
{
    private readonly IFacilityService _facilityService;
    private readonly ISoilService _soilService;
    private readonly INozzleService _nozzleService;
    private readonly ICropService _cropService;
    private readonly ISprayfieldService _sprayfieldService;
    private readonly IMonthlyApplicationService _monthlyApplicationService;
    private readonly IMonitoringWellService _monitoringWellService;
    private readonly ILookupQueryService _lookupQueryService;
    private readonly IBaselineMonthlyLoadingService _baselineMonthlyLoadingService;
    private readonly ApplicationDbContext _context;
    private readonly IPcsCatalogService _pcsCatalogService;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public SystemAdminController(
        IFacilityService facilityService,
        ISoilService soilService,
        INozzleService nozzleService,
        ICropService cropService,
        ISprayfieldService sprayfieldService,
        IMonthlyApplicationService monthlyApplicationService,
        IMonitoringWellService monitoringWellService,
        ILookupQueryService lookupQueryService,
        IBaselineMonthlyLoadingService baselineMonthlyLoadingService,
        ApplicationDbContext context,
        IPcsCatalogService pcsCatalogService,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        UserManager<ApplicationUser> userManager,
        ILogger<SystemAdminController> logger)
        : base(userManager, logger)
    {
        _facilityService = facilityService;
        _soilService = soilService;
        _nozzleService = nozzleService;
        _cropService = cropService;
        _sprayfieldService = sprayfieldService;
        _monthlyApplicationService = monthlyApplicationService;
        _monitoringWellService = monitoringWellService;
        _lookupQueryService = lookupQueryService;
        _baselineMonthlyLoadingService = baselineMonthlyLoadingService;
        _context = context;
        _pcsCatalogService = pcsCatalogService;
        _environment = environment;
        _configuration = configuration;
    }


    #region SystemAdmin (Tabbed Interface)
    [Authorize(Policy = Policies.RequireTechnicianOrOperator)]
    [HttpGet]
    public async Task<IActionResult> SystemAdmin(string tab = "facilities")
    {
        Guid? companyId = null;
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var canManageRecords = isGlobalAdmin || await IsInRoleAsync("company_admin");
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
        
        // Validate tab parameter
        var validTabs = new[] { "facilities", "soils", "crops", "nozzles", "sprayfields", "monitoringwells", "permits", "laboptions" };
        if (!validTabs.Contains(tab?.ToLower()))
        {
            tab = "facilities";
        }
        
        var viewModel = new ViewModels.SystemAdmin.SystemAdminViewModel
        {
            ActiveTab = tab.ToLower(),
            IsGlobalAdmin = isGlobalAdmin,
            CanManageRecords = canManageRecords,
            SelectedCompanyId = companyId,
            Companies = await GetCompanySelectListAsync()
        };
        
        // Switch statement to load appropriate data based on active tab
        switch (tab.ToLower())
        {
            case "facilities":
                var facilities = await _facilityService.GetAllAsync(companyId);
                viewModel.Facilities = facilities.Select(Gw59FacilityFieldResolver.ToFacilityListItemViewModel);
                viewModel.FacilitiesFilter = await CreateFacilitiesFilterViewModelAsync(isGlobalAdmin, companyId);
                break;
            case "soils":
                var soils = await _soilService.GetAllAsync(companyId);
                viewModel.Soils = soils.Select(s => new SoilViewModel
                {
                    Id = s.Id,
                    CompanyId = s.CompanyId,
                    CompanyName = s.Company?.Name,
                    TypeName = s.TypeName,
                    Description = s.Description,
                    Permeability = s.Permeability
                });
                viewModel.SoilsFilter = await CreateSoilsFilterViewModelAsync(isGlobalAdmin, companyId);
                break;
            case "crops":
                var crops = await _cropService.GetAllAsync(companyId);
                viewModel.Crops = crops.Select(c => new CropViewModel
                {
                    Id = c.Id,
                    CompanyId = c.CompanyId,
                    CompanyName = c.Company?.Name,
                    Name = c.Name,
                    NUptake = c.NUptake
                });
                viewModel.CropsFilter = await CreateCropsFilterViewModelAsync(isGlobalAdmin, companyId);
                break;
            case "nozzles":
                var nozzles = await _nozzleService.GetAllAsync(companyId);
                viewModel.Nozzles = nozzles.Select(n => new NozzleViewModel
                {
                    Id = n.Id,
                    CompanyId = n.CompanyId,
                    CompanyName = n.Company?.Name,
                    Model = n.Model,
                    Manufacturer = n.Manufacturer,
                    FlowRateGpm = n.FlowRateGpm,
                    SprayArc = n.SprayArc,
                    Comment=n.Comment
                });
                viewModel.NozzlesFilter = await CreateNozzlesFilterViewModelAsync(isGlobalAdmin, companyId);
                break;
            case "sprayfields":
                var sprayfields = await _sprayfieldService.GetAllAsync(companyId);
                viewModel.Sprayfields = sprayfields
                    .OrderBy(s => BuildNaturalSortKey(s.FieldId))
                    .ThenBy(s => s.FieldId)
                    .Select(s => new SprayfieldViewModel
                {
                    Id = s.Id,
                    CompanyId = s.CompanyId,
                    CompanyName = s.Company?.Name,
                    FieldId = s.FieldId,
                    SizeAcres = s.SizeAcres,
                    SoilName = SprayfieldZoneSummaryHelper.GetSoilSummary(s),
                    CropName = SprayfieldZoneSummaryHelper.GetCropSummary(s),
                    NozzleName = SprayfieldZoneSummaryHelper.GetNozzleSummary(s),
                    FacilityId = s.FacilityId,
                    FacilityName = s.Facility?.Name,
                    HydraulicLoadingLimitInPerYr = s.HydraulicLoadingLimitInPerYr,
                    HourlyRateInches = s.HourlyRateInches,
                    ActualHourlyRateInches = s.ActualHourlyRateInches,
                    AnnualRateInches = s.AnnualRateInches,
                    WeeklyRateInches = s.WeeklyRateInches
                });
                var facilitiesForBulkEdit = (await _facilityService.GetAllAsync(companyId))
                    .OrderBy(f => f.Name)
                    .ToList();

                viewModel.SprayfieldFacilities = new SelectList(facilitiesForBulkEdit, "Id", "Name");
                viewModel.SprayfieldsFilter = await CreateSprayfieldsFilterViewModelAsync(isGlobalAdmin, companyId);
                break;
            case "monitoringwells":
                var monitoringWells = await _monitoringWellService.GetAllAsync(companyId);
                viewModel.MonitoringWells = monitoringWells.Select(m => new MonitoringWellViewModel
                {
                    Id = m.Id,
                    CompanyId = m.CompanyId,
                    CompanyName = m.Company?.Name,
                    WellId = m.WellId,
                    WellPermitNumber = m.WellPermitNumber,
                    LocationDescription = m.LocationDescription,
                    DiameterInches = m.DiameterInches,
                    WellDepthFeet = m.WellDepthFeet,
                    DepthToScreenFeet = m.DepthToScreenFeet,
                    LowScreenDepthFeet = m.LowScreenDepthFeet,
                    HighScreenDepthFeet = m.HighScreenDepthFeet,
                    TopOfCasingElevationMsl = m.TopOfCasingElevationMsl,
                    TreatmentSystemLocation = m.TreatmentSystemLocation,
                    NumberOfWellsToBeSampled = m.NumberOfWellsToBeSampled,
                    Latitude = m.Latitude,
                    Longitude = m.Longitude
                });
                viewModel.MonitoringWellsFilter = await CreateMonitoringWellsFilterViewModelAsync(isGlobalAdmin, companyId);
                break;
            case "permits":
                var permitQuery = _context.FacilityPermits
                    .AsNoTracking()
                    .Include(p => p.Facility)
                    .AsQueryable();
                if (companyId.HasValue)
                {
                    permitQuery = permitQuery.Where(p => p.CompanyId == companyId.Value);
                }

                viewModel.Permits = await permitQuery
                    .OrderBy(p => p.Facility!.Name)
                    .ThenByDescending(p => p.EffectiveStartDate)
                    .Select(p => new PermitListItemViewModel
                    {
                        Id = p.Id,
                        FacilityId = p.FacilityId,
                        FacilityName = p.Facility!.Name,
                        PermitNumber = p.PermitNumber,
                        PermitVersion = p.PermitVersion,
                        EffectiveStartDate = p.EffectiveStartDate,
                        EffectiveEndDate = p.EffectiveEndDate,
                        IsActive = p.IsActive,
                        GwOperationLagoon = p.GwOperationLagoon,
                        GwOperationSprayField = p.GwOperationSprayField,
                        County = p.County,
                        Address = p.Address,
                        City = p.City,
                        State = p.State,
                        ZipCode = p.ZipCode,
                        TotalNumberOfSprayfields = p.TotalNumberOfSprayfields,
                        HasPdf = !string.IsNullOrWhiteSpace(p.PermitPdfStoragePath)
                    })
                    .ToListAsync();
                break;
            case "laboptions":
                var labOptions = await _context.CompanyLabOptions
                    .AsNoTracking()
                    .Include(x => x.Company)
                    .Where(x => x.IsActive)
                    .Where(x => !companyId.HasValue || x.CompanyId == companyId.Value)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name)
                    .ToListAsync();
                viewModel.LabOptions = labOptions.Select(x => new LabOptionViewModel
                {
                    Id = x.Id,
                    CompanyId = x.CompanyId,
                    CompanyName = x.Company?.Name,
                    Name = x.Name,
                    CertificationNumber = x.CertificationNumber,
                    SortOrder = x.SortOrder,
                    IsActive = x.IsActive
                });
                break;
        }
        
        return View(viewModel);
    }

    // Helper methods to create filter ViewModels
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    private async Task<FilterViewModel> CreateFacilitiesFilterViewModelAsync(bool isGlobalAdmin, Guid? companyId)
    {
        var filterViewModel = new FilterViewModel
        {
            PageName = "Facilities",
            EnableSearch = false,
            Fields = new List<FilterField>(),
            ActionName = "SystemAdmin",
            ControllerName = "SystemAdmin"
        };

        if (isGlobalAdmin)
        {
            var companies = await GetCompanySelectListAsync();
            filterViewModel.Fields.Add(new FilterField
            {
                Name = "companyId",
                Label = "Company",
                Type = FilterFieldType.Dropdown,
                Options = companies,
                Value = companyId,
                ColumnClass = "col-md-4",
                IconClass = "bi bi-building"
            });
        }

        return filterViewModel;
    }
    
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    private async Task<FilterViewModel> CreateSoilsFilterViewModelAsync(bool isGlobalAdmin, Guid? companyId)
    {
        var filterViewModel = new FilterViewModel
        {
            PageName = "Soils",
            EnableSearch = false,
            Fields = new List<FilterField>(),
            ActionName = "SystemAdmin",
            ControllerName = "SystemAdmin"
        };

        if (isGlobalAdmin)
        {
            var companies = await GetCompanySelectListAsync();
            filterViewModel.Fields.Add(new FilterField
            {
                Name = "companyId",
                Label = "Company",
                Type = FilterFieldType.Dropdown,
                Options = companies,
                Value = companyId,
                ColumnClass = "col-md-4",
                IconClass = "bi bi-building"
            });
        }

        return filterViewModel;
    }

    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    private async Task<FilterViewModel> CreateCropsFilterViewModelAsync(bool isGlobalAdmin, Guid? companyId)
    {
        var filterViewModel = new FilterViewModel
        {
            PageName = "Crops",
            EnableSearch = false,
            Fields = new List<FilterField>(),
            ActionName = "SystemAdmin",
            ControllerName = "SystemAdmin"
        };

        if (isGlobalAdmin)
        {
            var companies = await GetCompanySelectListAsync();
            filterViewModel.Fields.Add(new FilterField
            {
                Name = "companyId",
                Label = "Company",
                Type = FilterFieldType.Dropdown,
                Options = companies,
                Value = companyId,
                ColumnClass = "col-md-4",
                IconClass = "bi bi-building"
            });
        }

        return filterViewModel;
    }

    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    private async Task<FilterViewModel> CreateNozzlesFilterViewModelAsync(bool isGlobalAdmin, Guid? companyId)
    {
        var filterViewModel = new FilterViewModel
        {
            PageName = "Nozzles",
            EnableSearch = false,
            Fields = new List<FilterField>(),
            ActionName = "SystemAdmin",
            ControllerName = "SystemAdmin"
        };

        if (isGlobalAdmin)
        {
            var companies = await GetCompanySelectListAsync();
            filterViewModel.Fields.Add(new FilterField
            {
                Name = "companyId",
                Label = "Company",
                Type = FilterFieldType.Dropdown,
                Options = companies,
                Value = companyId,
                ColumnClass = "col-md-4",
                IconClass = "bi bi-building"
            });
        }

        return filterViewModel;
    }

    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    private async Task<FilterViewModel> CreateSprayfieldsFilterViewModelAsync(bool isGlobalAdmin, Guid? companyId)
    {
        var filterViewModel = new FilterViewModel
        {
            PageName = "Sprayfields",
            EnableSearch = false,
            Fields = new List<FilterField>(),
            ActionName = "SystemAdmin",
            ControllerName = "SystemAdmin"
        };

        if (isGlobalAdmin)
        {
            var companies = await GetCompanySelectListAsync();
            filterViewModel.Fields.Add(new FilterField
            {
                Name = "companyId",
                Label = "Company",
                Type = FilterFieldType.Dropdown,
                Options = companies,
                Value = companyId,
                ColumnClass = "col-md-4",
                IconClass = "bi bi-building"
            });
        }

        return filterViewModel;
    }

    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    private async Task<FilterViewModel> CreateMonitoringWellsFilterViewModelAsync(bool isGlobalAdmin, Guid? companyId)
    {
        var filterViewModel = new FilterViewModel
        {
            PageName = "MonitoringWells",
            EnableSearch = false,
            Fields = new List<FilterField>(),
            ActionName = "SystemAdmin",
            ControllerName = "SystemAdmin"
        };

        if (isGlobalAdmin)
        {
            var companies = await GetCompanySelectListAsync();
            filterViewModel.Fields.Add(new FilterField
            {
                Name = "companyId",
                Label = "Company",
                Type = FilterFieldType.Dropdown,
                Options = companies,
                Value = companyId,
                ColumnClass = "col-md-4",
                IconClass = "bi bi-building"
            });
        }

        return filterViewModel;
    }

    #endregion

    #region Helper Methods

    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    private async Task<SelectList> GetCompanySelectListAsync()
    {
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();
        var companies = await _lookupQueryService.GetCompaniesAsync(effectiveCompanyId);

        return new SelectList(companies, "Id", "Name");
    }

    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    private async Task PopulateSprayfieldDropdownsAsync(Guid companyId)
    {
        var soils = await _soilService.GetByCompanyIdAsync(companyId);
        var crops = await _cropService.GetByCompanyIdAsync(companyId);
        var nozzles = await _nozzleService.GetByCompanyIdAsync(companyId);
        var facilities = await _facilityService.GetByCompanyIdAsync(companyId);

        ViewBag.Soils = new SelectList(soils, "Id", "TypeName");
        ViewBag.Crops = new SelectList(crops, "Id", "Name");
        
        // Create nozzle select list with display text combining manufacturer and model
        var nozzleItems = nozzles.Select(n => new SelectListItem
        {
            Value = n.Id.ToString(),
            Text = $"{n.Manufacturer} {n.Model}"
        }).ToList();
        ViewBag.Nozzles = new SelectList(nozzleItems, "Value", "Text");
        
        ViewBag.Facilities = new SelectList(facilities, "Id", "Name");
        ViewBag.Companies = await GetCompanySelectListAsync();
    }

    private static string BuildNaturalSortKey(string? input)
    {
        return Regex.Replace(input ?? string.Empty, @"\d+", match => match.Value.PadLeft(10, '0'));
    }

    private async Task<SelectList> GetFacilityPermitSelectListAsync(Guid facilityId, Guid? selectedId)
    {
        var permits = await _context.FacilityPermits
            .AsNoTracking()
            .Where(p => p.FacilityId == facilityId && p.IsActive)
            .OrderByDescending(p => p.EffectiveStartDate)
            .Select(p => new { p.Id, Label = $"{p.PermitNumber} v{p.PermitVersion}" })
            .ToListAsync();

        return new SelectList(permits, "Id", "Label", selectedId);
    }

    private async Task<SelectList> GetLabOptionSelectListAsync(Guid companyId, Guid? selectedId)
    {
        var labs = await _context.CompanyLabOptions
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();

        return new SelectList(labs, "Id", "Name", selectedId);
    }

    private async Task<Guid?> ResolveSingleLabOptionIdAsync(Guid companyId)
    {
        var labIds = await _context.CompanyLabOptions
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync();

        return labIds.Count == 1 ? labIds[0] : null;
    }

    private async Task<Guid?> ResolveSingleFacilityPermitIdAsync(Guid facilityId)
    {
        var permitIds = await _context.FacilityPermits
            .AsNoTracking()
            .Where(p => p.FacilityId == facilityId && p.IsActive)
            .Select(p => p.Id)
            .ToListAsync();

        return permitIds.Count == 1 ? permitIds[0] : null;
    }

    #endregion
}
