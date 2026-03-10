using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.RegularExpressions;
using SAM.Controllers.Base;
using SAM.Domain.Entities;
using SAM.Infrastructure.Authorization;
using SAM.Services.Interfaces;
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
    private readonly IApplicationZoneService _applicationZoneService;
    private readonly IMonthlyApplicationService _monthlyApplicationService;
    private readonly IMonitoringWellService _monitoringWellService;
    private readonly ILookupQueryService _lookupQueryService;

    public SystemAdminController(
        IFacilityService facilityService,
        ISoilService soilService,
        INozzleService nozzleService,
        ICropService cropService,
        ISprayfieldService sprayfieldService,
        IApplicationZoneService applicationZoneService,
        IMonthlyApplicationService monthlyApplicationService,
        IMonitoringWellService monitoringWellService,
        ILookupQueryService lookupQueryService,
        UserManager<ApplicationUser> userManager,
        ILogger<SystemAdminController> logger)
        : base(userManager, logger)
    {
        _facilityService = facilityService;
        _soilService = soilService;
        _nozzleService = nozzleService;
        _cropService = cropService;
        _sprayfieldService = sprayfieldService;
        _applicationZoneService = applicationZoneService;
        _monthlyApplicationService = monthlyApplicationService;
        _monitoringWellService = monitoringWellService;
        _lookupQueryService = lookupQueryService;
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
        var validTabs = new[] { "facilities", "soils", "crops", "nozzles", "sprayfields", "monitoringwells" };
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
                viewModel.Facilities = facilities.Select(f => new FacilityViewModel
                {
                    Id = f.Id,
                    CompanyId = f.CompanyId,
                    CompanyName = f.Company?.Name,
                    Name = f.Name,
                    PermitNumber = f.PermitNumber,
                    Permittee = f.Permittee,
                    FacilityClass = f.FacilityClass,
                    Address = f.Address,
                    City = f.City,
                    State = f.State,
                    ZipCode = f.ZipCode,
                    County = f.County
                });
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
                    SoilId = s.SoilId,
                    SoilName = s.Soil?.TypeName,
                    CropId = s.CropId,
                    CropName = s.Crop?.Name,
                    NozzleId = s.NozzleId,
                    NozzleName = $"{s.Nozzle?.Manufacturer} {s.Nozzle?.Model}",
                    FacilityId = s.FacilityId,
                    FacilityName = s.Facility?.Name,
                    HydraulicLoadingLimitInPerYr = s.HydraulicLoadingLimitInPerYr,
                    HourlyRateInches = s.HourlyRateInches,
                    WeeklyRateInches = s.WeeklyRateInches
                });

                var soilsForBulkEdit = (await _soilService.GetAllAsync(companyId))
                    .OrderBy(s => s.TypeName)
                    .ToList();
                var cropsForBulkEdit = (await _cropService.GetAllAsync(companyId))
                    .OrderBy(c => c.Name)
                    .ToList();
                var nozzlesForBulkEdit = (await _nozzleService.GetAllAsync(companyId))
                    .OrderBy(n => n.Manufacturer)
                    .ThenBy(n => n.Model)
                    .ToList();
                var facilitiesForBulkEdit = (await _facilityService.GetAllAsync(companyId))
                    .OrderBy(f => f.Name)
                    .ToList();

                viewModel.SprayfieldSoils = new SelectList(soilsForBulkEdit, "Id", "TypeName");
                viewModel.SprayfieldCrops = new SelectList(cropsForBulkEdit, "Id", "Name");
                viewModel.SprayfieldNozzles = new SelectList(
                    nozzlesForBulkEdit.Select(n => new
                    {
                        n.Id,
                        Name = $"{n.Manufacturer} {n.Model}"
                    }),
                    "Id",
                    "Name");
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

    #endregion
}
