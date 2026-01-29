using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SAM.Controllers.Base;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Authorization;
using SAM.Services.Interfaces;
using SAM.ViewModels.Common;
using SAM.ViewModels.SystemAdmin;

namespace SAM.Controllers;

/// <summary>
/// Controller for System Administration module - managing reference data.
/// </summary>
[Authorize(Policy = Policies.RequireCompanyAdmin)]
public class SystemAdminController : BaseController
{
    private readonly ICompanyService _companyService;
    private readonly IFacilityService _facilityService;
    private readonly ISoilService _soilService;
    private readonly INozzleService _nozzleService;
    private readonly ICropService _cropService;
    private readonly ISprayfieldService _sprayfieldService;
    private readonly IMonitoringWellService _monitoringWellService;
    private readonly ApplicationDbContext _context;

    public SystemAdminController(
        ICompanyService companyService,
        IFacilityService facilityService,
        ISoilService soilService,
        INozzleService nozzleService,
        ICropService cropService,
        ISprayfieldService sprayfieldService,
        IMonitoringWellService monitoringWellService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<SystemAdminController> logger)
        : base(userManager, logger)
    {
        _companyService = companyService;
        _facilityService = facilityService;
        _soilService = soilService;
        _nozzleService = nozzleService;
        _cropService = cropService;
        _sprayfieldService = sprayfieldService;
        _monitoringWellService = monitoringWellService;
        _context = context;
    }

    #region Companies

    [HttpGet]
    public async Task<IActionResult> Companies(string? searchTerm)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        IEnumerable<Company> companies;

        // Apply search if search term is provided
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchFields = new[] { "Name", "ContactEmail", "PhoneNumber", "Website", "Description", "TaxId", "LicenseNumber" };
            companies = await _companyService.SearchAsync(searchTerm, searchFields);
        }
        else
        {
            companies = await _companyService.GetAllAsync();
        }
        
        // Filter by company if not global admin
        if (!isGlobalAdmin && effectiveCompanyId.HasValue)
        {
            companies = companies.Where(c => c.Id == effectiveCompanyId.Value);
        }

        var viewModels = companies.Select(c => new CompanyViewModel
        {
            Id = c.Id,
            Name = c.Name,
            ContactEmail = c.ContactEmail,
            PhoneNumber = c.PhoneNumber,
            Website = c.Website,
            Description = c.Description,
            TaxId = c.TaxId,
            FaxNumber = c.FaxNumber,
            LicenseNumber = c.LicenseNumber,
            IsActive = c.IsActive,
            IsVerified = c.IsVerified,
            CreatedDate = c.CreatedDate,
            UpdatedDate = c.UpdatedDate,
            CreatedBy = c.CreatedBy
        });

        // Create filter view model (search only for global admins)
        var filterViewModel = new FilterViewModel
        {
            PageName = "Companies",
            EnableSearch = isGlobalAdmin,
            SearchPlaceholder = "Search by name, email, phone, website...",
            SearchTerm = searchTerm
        };

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        ViewBag.FilterViewModel = filterViewModel;
        return View(viewModels);
    }

    [HttpGet]
    public async Task<IActionResult> CompanyDetails(Guid id)
    {
        var company = await _companyService.GetByIdAsync(id);
        if (company == null)
            return NotFound();

        // Check company access
        await EnsureCompanyAccessAsync(company.Id);

        var isGlobalAdmin = await IsGlobalAdminAsync();

        // Load Users with roles
        var users = await _context.Users
            .Where(u => u.CompanyId == id)
            .ToListAsync();

        var userViewModels = new List<UserListItemViewModel>();
        foreach (var user in users)
        {
            var roles = await UserManager.GetRolesAsync(user);
            userViewModels.Add(new UserListItemViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                IsActive = user.IsActive,
                Roles = roles.ToList()
            });
        }

        // Load Facilities
        var facilities = await _facilityService.GetAllAsync(id);
        var facilityViewModels = facilities.Select(f => new FacilityViewModel
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
        }).ToList();

        // Load Soils
        var soils = await _soilService.GetAllAsync(id);
        var soilViewModels = soils.Select(s => new SoilViewModel
        {
            Id = s.Id,
            CompanyId = s.CompanyId,
            CompanyName = s.Company?.Name,
            TypeName = s.TypeName,
            Description = s.Description,
            Permeability = s.Permeability
        }).ToList();

        // Load Crops
        var crops = await _cropService.GetAllAsync(id);
        var cropViewModels = crops.Select(c => new CropViewModel
        {
            Id = c.Id,
            CompanyId = c.CompanyId,
            CompanyName = c.Company?.Name,
            Name = c.Name,
            PanFactor = c.PanFactor,
            NUptake = c.NUptake
        }).ToList();

        // Load Nozzles
        var nozzles = await _nozzleService.GetAllAsync(id);
        var nozzleViewModels = nozzles.Select(n => new NozzleViewModel
        {
            Id = n.Id,
            CompanyId = n.CompanyId,
            CompanyName = n.Company?.Name,
            Model = n.Model,
            Manufacturer = n.Manufacturer,
            FlowRateGpm = n.FlowRateGpm,
            SprayArc = n.SprayArc
        }).ToList();

        // Load Sprayfields
        var sprayfields = await _sprayfieldService.GetAllAsync(id);
        var sprayfieldViewModels = sprayfields.Select(s => new SprayfieldViewModel
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
            HydraulicLoadingLimitInPerYr = s.HydraulicLoadingLimitInPerYr
        }).ToList();

        // Load Monitoring Wells
        var monitoringWells = await _monitoringWellService.GetAllAsync(id);
        var monitoringWellViewModels = monitoringWells.Select(m => new MonitoringWellViewModel
        {
            Id = m.Id,
            CompanyId = m.CompanyId,
            CompanyName = m.Company?.Name,
            WellId = m.WellId,
            LocationDescription = m.LocationDescription,
            Latitude = m.Latitude,
            Longitude = m.Longitude
        }).ToList();

        var viewModel = new CompanyDetailsViewModel
        {
            Id = company.Id,
            Name = company.Name,
            ContactEmail = company.ContactEmail,
            PhoneNumber = company.PhoneNumber,
            Website = company.Website,
            Description = company.Description,
            TaxId = company.TaxId,
            FaxNumber = company.FaxNumber,
            LicenseNumber = company.LicenseNumber,
            IsActive = company.IsActive,
            IsVerified = company.IsVerified,
            CreatedDate = company.CreatedDate,
            UpdatedDate = company.UpdatedDate,
            CreatedBy = company.CreatedBy,
            Users = userViewModels,
            Facilities = facilityViewModels,
            Soils = soilViewModels,
            Crops = cropViewModels,
            Nozzles = nozzleViewModels,
            Sprayfields = sprayfieldViewModels,
            MonitoringWells = monitoringWellViewModels,
            IsGlobalAdmin = isGlobalAdmin
        };

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireAdmin)]
    public IActionResult CompanyCreate()
    {
        return View(new CompanyCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> CompanyCreate(CompanyCreateViewModel viewModel)
    {
        if (!ModelState.IsValid)
            return View(viewModel);

        try
        {
            var company = new Company
            {
                Name = viewModel.Name,
                ContactEmail = viewModel.ContactEmail,
                PhoneNumber = viewModel.PhoneNumber,
                Website = viewModel.Website,
                Description = viewModel.Description,
                TaxId = viewModel.TaxId,
                FaxNumber = viewModel.FaxNumber,
                LicenseNumber = viewModel.LicenseNumber,
                IsActive = viewModel.IsActive,
                IsVerified = viewModel.IsVerified
            };

            await _companyService.CreateAsync(company);
            TempData["SuccessMessage"] = $"Company '{company.Name}' created successfully.";
            return RedirectToAction(nameof(Companies));
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(viewModel);
        }
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> CompanyEdit(Guid id)
    {
        var company = await _companyService.GetByIdAsync(id);
        if (company == null)
            return NotFound();

        var viewModel = new CompanyEditViewModel
        {
            Id = company.Id,
            Name = company.Name,
            ContactEmail = company.ContactEmail,
            PhoneNumber = company.PhoneNumber,
            Website = company.Website,
            Description = company.Description,
            TaxId = company.TaxId,
            FaxNumber = company.FaxNumber,
            LicenseNumber = company.LicenseNumber,
            IsActive = company.IsActive,
            IsVerified = company.IsVerified
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> CompanyEdit(CompanyEditViewModel viewModel)
    {
        if (!ModelState.IsValid)
            return View(viewModel);

        try
        {
            var company = await _companyService.GetByIdAsync(viewModel.Id);
            if (company == null)
                return NotFound();

            company.Name = viewModel.Name;
            company.ContactEmail = viewModel.ContactEmail;
            company.PhoneNumber = viewModel.PhoneNumber;
            company.Website = viewModel.Website;
            company.Description = viewModel.Description;
            company.TaxId = viewModel.TaxId;
            company.FaxNumber = viewModel.FaxNumber;
            company.LicenseNumber = viewModel.LicenseNumber;
            company.IsActive = viewModel.IsActive;
            company.IsVerified = viewModel.IsVerified;

            await _companyService.UpdateAsync(company);
            TempData["SuccessMessage"] = $"Company '{company.Name}' updated successfully.";
            return RedirectToAction(nameof(Companies));
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(viewModel);
        }
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> CompanyDelete(Guid id)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (company == null)
            return NotFound();

        // Count related entities
        var usersCount = await _context.Users
            .CountAsync(u => u.CompanyId == id);
        
        var facilitiesCount = await _context.Facilities
            .CountAsync(f => f.CompanyId == id && !f.IsDeleted);
        
        var soilsCount = await _context.Soils
            .CountAsync(s => s.CompanyId == id && !s.IsDeleted);
        
        var cropsCount = await _context.Crops
            .CountAsync(c => c.CompanyId == id && !c.IsDeleted);
        
        var nozzlesCount = await _context.Nozzles
            .CountAsync(n => n.CompanyId == id && !n.IsDeleted);
        
        var sprayfieldsCount = await _context.Sprayfields
            .CountAsync(s => s.CompanyId == id && !s.IsDeleted);
        
        var monitoringWellsCount = await _context.MonitoringWells
            .CountAsync(m => m.CompanyId == id && !m.IsDeleted);
        
        var userRequestsCount = await _context.UserRequests
            .CountAsync(u => u.CompanyId == id && !u.IsDeleted);

        // Get detailed lists for critical entities
        var users = await _context.Users
            .Where(u => u.CompanyId == id)
            .Select(u => new ViewModels.SystemAdmin.UserInfo
            {
                Email = u.Email ?? string.Empty,
                FullName = u.FullName
            })
            .ToListAsync();

        var facilities = await _context.Facilities
            .Where(f => f.CompanyId == id && !f.IsDeleted)
            .Select(f => new ViewModels.SystemAdmin.FacilityInfo
            {
                Name = f.Name,
                PermitNumber = f.PermitNumber
            })
            .ToListAsync();

        var userRequests = await _context.UserRequests
            .Where(u => u.CompanyId == id && !u.IsDeleted)
            .Select(u => new ViewModels.SystemAdmin.UserRequestInfo
            {
                Email = u.Email,
                FullName = u.FullName,
                Status = u.Status.ToString()
            })
            .ToListAsync();

        var viewModel = new CompanyDeleteViewModel
        {
            Id = company.Id,
            Name = company.Name,
            ContactEmail = company.ContactEmail,
            PhoneNumber = company.PhoneNumber,
            Website = company.Website,
            Description = company.Description,
            CreatedDate = company.CreatedDate,
            UsersCount = usersCount,
            FacilitiesCount = facilitiesCount,
            SoilsCount = soilsCount,
            CropsCount = cropsCount,
            NozzlesCount = nozzlesCount,
            SprayfieldsCount = sprayfieldsCount,
            MonitoringWellsCount = monitoringWellsCount,
            UserRequestsCount = userRequestsCount,
            Users = users,
            Facilities = facilities,
            UserRequests = userRequests
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
    [ActionName("CompanyDelete")]
    public async Task<IActionResult> CompanyDeletePost(Guid id)
    {
        try
        {
            await _companyService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Company deleted successfully.";
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "Company not found.";
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Companies));
    }

    #endregion

    #region Facilities

    [HttpGet]
    public async Task<IActionResult> Facilities(Guid? companyId = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // Use effective company ID if not global admin and no companyId specified
        if (!isGlobalAdmin && !companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        // Ensure company access if companyId is specified
        if (companyId.HasValue)
        {
            await EnsureCompanyAccessAsync(companyId.Value);
        }

        var facilities = await _facilityService.GetAllAsync(companyId);
        
        var viewModels = facilities.Select(f => new FacilityViewModel
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

        // Create filter view model
        var filterViewModel = new FilterViewModel
        {
            PageName = "Facilities",
            EnableSearch = false,
            Fields = new List<FilterField>()
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

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        ViewBag.Companies = await GetCompanySelectListAsync();
        ViewBag.SelectedCompanyId = companyId;
        ViewBag.FilterViewModel = filterViewModel;

        return View(viewModels);
    }

    [HttpGet]
    public async Task<IActionResult> FacilityDetails(Guid id)
    {
        var facility = await _facilityService.GetByIdAsync(id);
        if (facility == null)
            return NotFound();

        // Check company access
        await EnsureCompanyAccessAsync(facility.CompanyId);

        var viewModel = new FacilityViewModel
        {
            Id = facility.Id,
            CompanyId = facility.CompanyId,
            CompanyName = facility.Company?.Name,
            Name = facility.Name,
            PermitNumber = facility.PermitNumber,
            Permittee = facility.Permittee,
            FacilityClass = facility.FacilityClass,
            Address = facility.Address,
            City = facility.City,
            State = facility.State,
            ZipCode = facility.ZipCode,
            County = facility.County
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> FacilityCreate(Guid? companyId = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // Set company ID if not provided
        if (!companyId.HasValue && !isGlobalAdmin && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        if (companyId.HasValue)
        {
            await EnsureCompanyAccessAsync(companyId.Value);
        }

        var viewModel = new FacilityCreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FacilityCreate(FacilityCreateViewModel viewModel)
    {
        // Ensure company access
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }

        try
        {
            var facility = new Facility
            {
                CompanyId = viewModel.CompanyId,
                Name = viewModel.Name,
                PermitNumber = viewModel.PermitNumber,
                Permittee = viewModel.Permittee,
                FacilityClass = viewModel.FacilityClass,
                Address = viewModel.Address,
                City = viewModel.City,
                State = viewModel.State,
                ZipCode = viewModel.ZipCode,
                County = viewModel.County
            };

            await _facilityService.CreateAsync(facility);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, id = facility.Id, name = facility.Name });
            }

            TempData["SuccessMessage"] = $"Facility '{facility.Name}' created successfully.";
            return RedirectToAction(nameof(Facilities), new { companyId = facility.CompanyId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Return partial form with validation errors for modal
                ViewBag.Companies = await GetCompanySelectListAsync();
                return PartialView("Partials/_FacilityCreateFormPartial", viewModel);
            }

            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }
    }

    [HttpGet]
    public async Task<IActionResult> FacilityEdit(Guid id)
    {
        var facility = await _facilityService.GetByIdAsync(id);
        if (facility == null)
            return NotFound();

        // Check company access
        await EnsureCompanyAccessAsync(facility.CompanyId);

        var viewModel = new FacilityEditViewModel
        {
            Id = facility.Id,
            CompanyId = facility.CompanyId,
            Name = facility.Name,
            PermitNumber = facility.PermitNumber,
            Permittee = facility.Permittee,
            FacilityClass = facility.FacilityClass,
            Address = facility.Address,
            City = facility.City,
            State = facility.State,
            ZipCode = facility.ZipCode,
            County = facility.County
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FacilityEdit(FacilityEditViewModel viewModel)
    {
        // Ensure company access
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }

        try
        {
            var facility = await _facilityService.GetByIdAsync(viewModel.Id);
            if (facility == null)
                return NotFound();

            facility.Name = viewModel.Name;
            facility.PermitNumber = viewModel.PermitNumber;
            facility.Permittee = viewModel.Permittee;
            facility.FacilityClass = viewModel.FacilityClass;
            facility.Address = viewModel.Address;
            facility.City = viewModel.City;
            facility.State = viewModel.State;
            facility.ZipCode = viewModel.ZipCode;
            facility.County = viewModel.County;

            await _facilityService.UpdateAsync(facility);
            TempData["SuccessMessage"] = $"Facility '{facility.Name}' updated successfully.";
            return RedirectToAction(nameof(Facilities), new { companyId = facility.CompanyId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FacilityDelete(Guid id)
    {
        try
        {
            var facility = await _facilityService.GetByIdAsync(id);
            if (facility != null)
            {
                await EnsureCompanyAccessAsync(facility.CompanyId);
            }

            await _facilityService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Facility deleted successfully.";
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "Facility not found.";
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Facilities));
    }

    #endregion

    #region Soils

    [HttpGet]
    public async Task<IActionResult> Soils(Guid? companyId = null)
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

        var soils = await _soilService.GetAllAsync(companyId);
        
        var viewModels = soils.Select(s => new SoilViewModel
        {
            Id = s.Id,
            CompanyId = s.CompanyId,
            CompanyName = s.Company?.Name,
            TypeName = s.TypeName,
            Description = s.Description,
            Permeability = s.Permeability
        });

        // Create filter view model
        var filterViewModel = new FilterViewModel
        {
            PageName = "Soils",
            EnableSearch = false,
            Fields = new List<FilterField>()
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

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        ViewBag.Companies = await GetCompanySelectListAsync();
        ViewBag.SelectedCompanyId = companyId;
        ViewBag.FilterViewModel = filterViewModel;

        return View(viewModels);
    }

    [HttpGet]
    public async Task<IActionResult> SoilCreate(Guid? companyId = null)
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

        var viewModel = new SoilCreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SoilCreate(SoilCreateViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Partials/_SoilCreateFormPartial", viewModel);
            }
            return View(viewModel);
        }

        try
        {
            var soil = new Soil
            {
                CompanyId = viewModel.CompanyId,
                TypeName = viewModel.TypeName,
                Description = viewModel.Description,
                Permeability = viewModel.Permeability
            };

            await _soilService.CreateAsync(soil);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, id = soil.Id, name = soil.TypeName });
            }

            TempData["SuccessMessage"] = $"Soil type '{soil.TypeName}' created successfully.";
            return RedirectToAction(nameof(Soils), new { companyId = soil.CompanyId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Partials/_SoilCreateFormPartial", viewModel);
            }
            return View(viewModel);
        }
    }

    [HttpGet]
    public async Task<IActionResult> SoilEdit(Guid id)
    {
        var soil = await _soilService.GetByIdAsync(id);
        if (soil == null)
            return NotFound();

        await EnsureCompanyAccessAsync(soil.CompanyId);

        var viewModel = new SoilEditViewModel
        {
            Id = soil.Id,
            CompanyId = soil.CompanyId,
            TypeName = soil.TypeName,
            Description = soil.Description,
            Permeability = soil.Permeability
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SoilEdit(SoilEditViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }

        try
        {
            var soil = await _soilService.GetByIdAsync(viewModel.Id);
            if (soil == null)
                return NotFound();

            soil.TypeName = viewModel.TypeName;
            soil.Description = viewModel.Description;
            soil.Permeability = viewModel.Permeability;

            await _soilService.UpdateAsync(soil);
            TempData["SuccessMessage"] = $"Soil type '{soil.TypeName}' updated successfully.";
            return RedirectToAction(nameof(Soils), new { companyId = soil.CompanyId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SoilDelete(Guid id)
    {
        try
        {
            var soil = await _soilService.GetByIdAsync(id);
            if (soil != null)
            {
                await EnsureCompanyAccessAsync(soil.CompanyId);
            }

            await _soilService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Soil type deleted successfully.";
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "Soil type not found.";
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Soils));
    }

    #endregion

    #region Crops

    [HttpGet]
    public async Task<IActionResult> Crops(Guid? companyId = null)
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

        var crops = await _cropService.GetAllAsync(companyId);
        
        var viewModels = crops.Select(c => new CropViewModel
        {
            Id = c.Id,
            CompanyId = c.CompanyId,
            CompanyName = c.Company?.Name,
            Name = c.Name,
            PanFactor = c.PanFactor,
            NUptake = c.NUptake
        });

        // Create filter view model
        var filterViewModel = new FilterViewModel
        {
            PageName = "Crops",
            EnableSearch = false,
            Fields = new List<FilterField>()
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

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        ViewBag.Companies = await GetCompanySelectListAsync();
        ViewBag.SelectedCompanyId = companyId;
        ViewBag.FilterViewModel = filterViewModel;

        return View(viewModels);
    }

    [HttpGet]
    public async Task<IActionResult> CropCreate(Guid? companyId = null)
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

        var viewModel = new CropCreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CropCreate(CropCreateViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Partials/_CropCreateFormPartial", viewModel);
            }

            return View(viewModel);
        }

        try
        {
            var crop = new Crop
            {
                CompanyId = viewModel.CompanyId,
                Name = viewModel.Name,
                PanFactor = viewModel.PanFactor,
                NUptake = viewModel.NUptake
            };

            await _cropService.CreateAsync(crop);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, id = crop.Id, name = crop.Name });
            }

            TempData["SuccessMessage"] = $"Crop '{crop.Name}' created successfully.";
            return RedirectToAction(nameof(Crops), new { companyId = crop.CompanyId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Partials/_CropCreateFormPartial", viewModel);
            }

            return View(viewModel);
        }
    }

    [HttpGet]
    public async Task<IActionResult> CropEdit(Guid id)
    {
        var crop = await _cropService.GetByIdAsync(id);
        if (crop == null)
            return NotFound();

        await EnsureCompanyAccessAsync(crop.CompanyId);

        var viewModel = new CropEditViewModel
        {
            Id = crop.Id,
            CompanyId = crop.CompanyId,
            Name = crop.Name,
            PanFactor = crop.PanFactor,
            NUptake = crop.NUptake
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CropEdit(CropEditViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }

        try
        {
            var crop = await _cropService.GetByIdAsync(viewModel.Id);
            if (crop == null)
                return NotFound();

            crop.Name = viewModel.Name;
            crop.PanFactor = viewModel.PanFactor;
            crop.NUptake = viewModel.NUptake;

            await _cropService.UpdateAsync(crop);
            TempData["SuccessMessage"] = $"Crop '{crop.Name}' updated successfully.";
            return RedirectToAction(nameof(Crops), new { companyId = crop.CompanyId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CropDelete(Guid id)
    {
        try
        {
            var crop = await _cropService.GetByIdAsync(id);
            if (crop != null)
            {
                await EnsureCompanyAccessAsync(crop.CompanyId);
            }

            await _cropService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Crop deleted successfully.";
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "Crop not found.";
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Crops));
    }

    #endregion

    #region Nozzles

    [HttpGet]
    public async Task<IActionResult> Nozzles(Guid? companyId = null)
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

        var nozzles = await _nozzleService.GetAllAsync(companyId);
        
        var viewModels = nozzles.Select(n => new NozzleViewModel
        {
            Id = n.Id,
            CompanyId = n.CompanyId,
            CompanyName = n.Company?.Name,
            Model = n.Model,
            Manufacturer = n.Manufacturer,
            FlowRateGpm = n.FlowRateGpm,
            SprayArc = n.SprayArc
        });

        // Create filter view model
        var filterViewModel = new FilterViewModel
        {
            PageName = "Nozzles",
            EnableSearch = false,
            Fields = new List<FilterField>()
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

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        ViewBag.Companies = await GetCompanySelectListAsync();
        ViewBag.SelectedCompanyId = companyId;
        ViewBag.FilterViewModel = filterViewModel;

        return View(viewModels);
    }

    [HttpGet]
    public async Task<IActionResult> NozzleCreate(Guid? companyId = null)
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

        var viewModel = new NozzleCreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NozzleCreate(NozzleCreateViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Partials/_NozzleCreateFormPartial", viewModel);
            }

            return View(viewModel);
        }

        try
        {
            var nozzle = new Nozzle
            {
                CompanyId = viewModel.CompanyId,
                Model = viewModel.Model,
                Manufacturer = viewModel.Manufacturer,
                FlowRateGpm = viewModel.FlowRateGpm,
                SprayArc = viewModel.SprayArc
            };

            await _nozzleService.CreateAsync(nozzle);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var label = $"{nozzle.Manufacturer} {nozzle.Model}";
                return Json(new { success = true, id = nozzle.Id, name = label });
            }

            TempData["SuccessMessage"] = $"Nozzle '{nozzle.Manufacturer} {nozzle.Model}' created successfully.";
            return RedirectToAction(nameof(Nozzles), new { companyId = nozzle.CompanyId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Partials/_NozzleCreateFormPartial", viewModel);
            }

            return View(viewModel);
        }
    }

    [HttpGet]
    public async Task<IActionResult> NozzleEdit(Guid id)
    {
        var nozzle = await _nozzleService.GetByIdAsync(id);
        if (nozzle == null)
            return NotFound();

        await EnsureCompanyAccessAsync(nozzle.CompanyId);

        var viewModel = new NozzleEditViewModel
        {
            Id = nozzle.Id,
            CompanyId = nozzle.CompanyId,
            Model = nozzle.Model,
            Manufacturer = nozzle.Manufacturer,
            FlowRateGpm = nozzle.FlowRateGpm,
            SprayArc = nozzle.SprayArc
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NozzleEdit(NozzleEditViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }

        try
        {
            var nozzle = await _nozzleService.GetByIdAsync(viewModel.Id);
            if (nozzle == null)
                return NotFound();

            nozzle.Model = viewModel.Model;
            nozzle.Manufacturer = viewModel.Manufacturer;
            nozzle.FlowRateGpm = viewModel.FlowRateGpm;
            nozzle.SprayArc = viewModel.SprayArc;

            await _nozzleService.UpdateAsync(nozzle);
            TempData["SuccessMessage"] = $"Nozzle '{nozzle.Manufacturer} {nozzle.Model}' updated successfully.";
            return RedirectToAction(nameof(Nozzles), new { companyId = nozzle.CompanyId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NozzleDelete(Guid id)
    {
        try
        {
            var nozzle = await _nozzleService.GetByIdAsync(id);
            if (nozzle != null)
            {
                await EnsureCompanyAccessAsync(nozzle.CompanyId);
            }

            await _nozzleService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Nozzle deleted successfully.";
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "Nozzle not found.";
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Nozzles));
    }

    #endregion

    #region Sprayfields

    [HttpGet]
    public async Task<IActionResult> Sprayfields(Guid? companyId = null)
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

        var sprayfields = await _sprayfieldService.GetAllAsync(companyId);
        
        var viewModels = sprayfields.Select(s => new SprayfieldViewModel
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
            AnnualRateInches = s.AnnualRateInches
        });

        // Create filter view model
        var filterViewModel = new FilterViewModel
        {
            PageName = "Sprayfields",
            EnableSearch = false,
            Fields = new List<FilterField>()
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

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        ViewBag.Companies = await GetCompanySelectListAsync();
        ViewBag.SelectedCompanyId = companyId;
        ViewBag.FilterViewModel = filterViewModel;

        return View(viewModels);
    }

    [HttpGet]
    public async Task<IActionResult> SprayfieldDetails(Guid id)
    {
        var sprayfield = await _sprayfieldService.GetByIdAsync(id);
        if (sprayfield == null)
            return NotFound();

        await EnsureCompanyAccessAsync(sprayfield.CompanyId);

        var viewModel = new SprayfieldViewModel
        {
            Id = sprayfield.Id,
            CompanyId = sprayfield.CompanyId,
            CompanyName = sprayfield.Company?.Name,
            FieldId = sprayfield.FieldId,
            SizeAcres = sprayfield.SizeAcres,
            SoilId = sprayfield.SoilId,
            SoilName = sprayfield.Soil?.TypeName,
            CropId = sprayfield.CropId,
            CropName = sprayfield.Crop?.Name,
            NozzleId = sprayfield.NozzleId,
            NozzleName = $"{sprayfield.Nozzle?.Manufacturer} {sprayfield.Nozzle?.Model}",
            FacilityId = sprayfield.FacilityId,
            FacilityName = sprayfield.Facility?.Name,
            HydraulicLoadingLimitInPerYr = sprayfield.HydraulicLoadingLimitInPerYr,
            HourlyRateInches = sprayfield.HourlyRateInches,
            AnnualRateInches = sprayfield.AnnualRateInches
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> SprayfieldCreate(Guid? companyId = null)
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

        var viewModel = new SprayfieldCreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty
        };

        await PopulateSprayfieldDropdownsAsync(viewModel.CompanyId);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SprayfieldCreate(SprayfieldCreateViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            await PopulateSprayfieldDropdownsAsync(viewModel.CompanyId);
            return View(viewModel);
        }

        try
        {
            var sprayfield = new Sprayfield
            {
                CompanyId = viewModel.CompanyId,
                FieldId = viewModel.FieldId,
                SizeAcres = viewModel.SizeAcres,
                SoilId = viewModel.SoilId,
                CropId = viewModel.CropId,
                NozzleId = viewModel.NozzleId,
                FacilityId = viewModel.FacilityId,
                HydraulicLoadingLimitInPerYr = viewModel.HydraulicLoadingLimitInPerYr,
                HourlyRateInches = viewModel.HourlyRateInches,
                AnnualRateInches = viewModel.AnnualRateInches
            };

            await _sprayfieldService.CreateAsync(sprayfield);
            TempData["SuccessMessage"] = $"Sprayfield '{sprayfield.FieldId}' created successfully.";
            return RedirectToAction(nameof(Sprayfields), new { companyId = sprayfield.CompanyId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await PopulateSprayfieldDropdownsAsync(viewModel.CompanyId);
            return View(viewModel);
        }
    }

    [HttpGet]
    public async Task<IActionResult> SprayfieldEdit(Guid id)
    {
        var sprayfield = await _sprayfieldService.GetByIdAsync(id);
        if (sprayfield == null)
            return NotFound();

        await EnsureCompanyAccessAsync(sprayfield.CompanyId);

        var viewModel = new SprayfieldEditViewModel
        {
            Id = sprayfield.Id,
            CompanyId = sprayfield.CompanyId,
            FieldId = sprayfield.FieldId,
            SizeAcres = sprayfield.SizeAcres,
            SoilId = sprayfield.SoilId,
            CropId = sprayfield.CropId,
            NozzleId = sprayfield.NozzleId,
            FacilityId = sprayfield.FacilityId,
            HydraulicLoadingLimitInPerYr = sprayfield.HydraulicLoadingLimitInPerYr,
            HourlyRateInches = sprayfield.HourlyRateInches,
            AnnualRateInches = sprayfield.AnnualRateInches
        };

        await PopulateSprayfieldDropdownsAsync(sprayfield.CompanyId);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SprayfieldEdit(SprayfieldEditViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            await PopulateSprayfieldDropdownsAsync(viewModel.CompanyId);
            return View(viewModel);
        }

        try
        {
            var sprayfield = await _sprayfieldService.GetByIdAsync(viewModel.Id);
            if (sprayfield == null)
                return NotFound();

            sprayfield.FieldId = viewModel.FieldId;
            sprayfield.SizeAcres = viewModel.SizeAcres;
            sprayfield.SoilId = viewModel.SoilId;
            sprayfield.CropId = viewModel.CropId;
            sprayfield.NozzleId = viewModel.NozzleId;
            sprayfield.FacilityId = viewModel.FacilityId;
            sprayfield.HydraulicLoadingLimitInPerYr = viewModel.HydraulicLoadingLimitInPerYr;
            sprayfield.HourlyRateInches = viewModel.HourlyRateInches;
            sprayfield.AnnualRateInches = viewModel.AnnualRateInches;

            await _sprayfieldService.UpdateAsync(sprayfield);
            TempData["SuccessMessage"] = $"Sprayfield '{sprayfield.FieldId}' updated successfully.";
            return RedirectToAction(nameof(Sprayfields), new { companyId = sprayfield.CompanyId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await PopulateSprayfieldDropdownsAsync(viewModel.CompanyId);
            return View(viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SprayfieldDelete(Guid id)
    {
        try
        {
            var sprayfield = await _sprayfieldService.GetByIdAsync(id);
            if (sprayfield != null)
            {
                await EnsureCompanyAccessAsync(sprayfield.CompanyId);
            }

            await _sprayfieldService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Sprayfield deleted successfully.";
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "Sprayfield not found.";
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Sprayfields));
    }

    #endregion

    #region Monitoring Wells

    [HttpGet]
    public async Task<IActionResult> MonitoringWells(Guid? companyId = null)
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

        var monitoringWells = await _monitoringWellService.GetAllAsync(companyId);
        
        var viewModels = monitoringWells.Select(m => new MonitoringWellViewModel
        {
            Id = m.Id,
            CompanyId = m.CompanyId,
            CompanyName = m.Company?.Name,
            WellId = m.WellId,
            LocationDescription = m.LocationDescription,
            Latitude = m.Latitude,
            Longitude = m.Longitude
        });

        // Create filter view model
        var filterViewModel = new FilterViewModel
        {
            PageName = "MonitoringWells",
            EnableSearch = false,
            Fields = new List<FilterField>()
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

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        ViewBag.Companies = await GetCompanySelectListAsync();
        ViewBag.SelectedCompanyId = companyId;
        ViewBag.FilterViewModel = filterViewModel;

        return View(viewModels);
    }

    [HttpGet]
    public async Task<IActionResult> MonitoringWellDetails(Guid id)
    {
        var monitoringWell = await _monitoringWellService.GetByIdAsync(id);
        if (monitoringWell == null)
            return NotFound();

        await EnsureCompanyAccessAsync(monitoringWell.CompanyId);

        var viewModel = new MonitoringWellViewModel
        {
            Id = monitoringWell.Id,
            CompanyId = monitoringWell.CompanyId,
            CompanyName = monitoringWell.Company?.Name,
            WellId = monitoringWell.WellId,
            LocationDescription = monitoringWell.LocationDescription,
            Latitude = monitoringWell.Latitude,
            Longitude = monitoringWell.Longitude
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> MonitoringWellCreate(Guid? companyId = null)
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

        var viewModel = new MonitoringWellCreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MonitoringWellCreate(MonitoringWellCreateViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }

        try
        {
            var monitoringWell = new MonitoringWell
            {
                CompanyId = viewModel.CompanyId,
                WellId = viewModel.WellId,
                LocationDescription = viewModel.LocationDescription,
                Latitude = viewModel.Latitude,
                Longitude = viewModel.Longitude
            };

            await _monitoringWellService.CreateAsync(monitoringWell);
            TempData["SuccessMessage"] = $"Monitoring well '{monitoringWell.WellId}' created successfully.";
            return RedirectToAction(nameof(MonitoringWells), new { companyId = monitoringWell.CompanyId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }
    }

    [HttpGet]
    public async Task<IActionResult> MonitoringWellEdit(Guid id)
    {
        var monitoringWell = await _monitoringWellService.GetByIdAsync(id);
        if (monitoringWell == null)
            return NotFound();

        await EnsureCompanyAccessAsync(monitoringWell.CompanyId);

        var viewModel = new MonitoringWellEditViewModel
        {
            Id = monitoringWell.Id,
            CompanyId = monitoringWell.CompanyId,
            WellId = monitoringWell.WellId,
            LocationDescription = monitoringWell.LocationDescription,
            Latitude = monitoringWell.Latitude,
            Longitude = monitoringWell.Longitude
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MonitoringWellEdit(MonitoringWellEditViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }

        try
        {
            var monitoringWell = await _monitoringWellService.GetByIdAsync(viewModel.Id);
            if (monitoringWell == null)
                return NotFound();

            monitoringWell.WellId = viewModel.WellId;
            monitoringWell.LocationDescription = viewModel.LocationDescription;
            monitoringWell.Latitude = viewModel.Latitude;
            monitoringWell.Longitude = viewModel.Longitude;

            await _monitoringWellService.UpdateAsync(monitoringWell);
            TempData["SuccessMessage"] = $"Monitoring well '{monitoringWell.WellId}' updated successfully.";
            return RedirectToAction(nameof(MonitoringWells), new { companyId = monitoringWell.CompanyId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MonitoringWellDelete(Guid id)
    {
        try
        {
            var monitoringWell = await _monitoringWellService.GetByIdAsync(id);
            if (monitoringWell != null)
            {
                await EnsureCompanyAccessAsync(monitoringWell.CompanyId);
            }

            await _monitoringWellService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Monitoring well deleted successfully.";
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "Monitoring well not found.";
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(MonitoringWells));
    }

    #endregion

    #region Helper Methods

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

    #endregion
}

