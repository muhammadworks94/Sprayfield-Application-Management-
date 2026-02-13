using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAM.Controllers.Base;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Authorization;
using SAM.Services.Interfaces;
using SAM.ViewModels.Common;
using SAM.ViewModels.SystemAdmin;
using SAM.ViewModels.UserManagement;

namespace SAM.Controllers;

/// <summary>
/// Controller for Company Management module - managing companies and company requests.
/// 
/// Company Creation Flows:
/// 1. Admin Direct Creation: Admins can directly create companies via CompanyCreate action (no request flow)
/// 2. Signup Requests: Users sign up via AccountController.Signup which creates CompanyRequest entities.
///    These requests are approved/rejected via CompanyRequestApprove/CompanyRequestReject actions in this controller.
/// </summary>
[Authorize(Policy = Policies.RequireAdmin)]
public class CompanyManagementController : BaseController
{
    private readonly ICompanyService _companyService;
    private readonly ICompanyRequestService _companyRequestService;
    private readonly IFacilityService _facilityService;
    private readonly ISoilService _soilService;
    private readonly INozzleService _nozzleService;
    private readonly ICropService _cropService;
    private readonly ISprayfieldService _sprayfieldService;
    private readonly IMonitoringWellService _monitoringWellService;
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;

    public CompanyManagementController(
        ICompanyService companyService,
        ICompanyRequestService companyRequestService,
        IFacilityService facilityService,
        ISoilService soilService,
        INozzleService nozzleService,
        ICropService cropService,
        ISprayfieldService sprayfieldService,
        IMonitoringWellService monitoringWellService,
        ApplicationDbContext context,
        IUserService userService,
        UserManager<ApplicationUser> userManager,
        ILogger<CompanyManagementController> logger)
        : base(userManager, logger)
    {
        _companyService = companyService;
        _companyRequestService = companyRequestService;
        _facilityService = facilityService;
        _soilService = soilService;
        _nozzleService = nozzleService;
        _cropService = cropService;
        _sprayfieldService = sprayfieldService;
        _monitoringWellService = monitoringWellService;
        _context = context;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // Load companies data
        IEnumerable<Company> companies;
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchFields = new[] { "Name", "ContactEmail", "PhoneNumber", "Website", "Description", "TaxId", "LicenseNumber" };
            companies = await _companyService.SearchAsync(searchTerm, searchFields);
        }
        else
        {
            companies = await _companyService.GetAllAsync();
        }

        // Filter by company if session has a selection (for admins) or user has a company
        if (effectiveCompanyId.HasValue)
        {
            companies = companies.Where(c => c.Id == effectiveCompanyId.Value);
        }

        var companyViewModels = companies.Select(c => new CompanyViewModel
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
        ViewBag.Companies = companyViewModels;

        return View();
    }

    /// <summary>
    /// Display all company requests from user signup.
    /// Note: This is for signup requests only. Admins create companies directly via CompanyCreate action.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CompanyRequests()
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();

        // Load company requests data
        var requests = await _companyRequestService.GetAllAsync();
        var companyRequestViewModels = requests.Select(r => new CompanyRequestViewModel
        {
            Id = r.Id,
            CompanyName = r.CompanyName,
            ContactEmail = r.ContactEmail,
            PhoneNumber = r.PhoneNumber,
            Website = r.Website,
            Description = r.Description,
            RequesterFullName = r.RequesterFullName,
            RequesterEmail = r.RequesterEmail,
            Status = r.Status,
            CreatedDate = r.CreatedDate,
            CreatedCompanyId = r.CreatedCompanyId,
            CreatedUserId = r.CreatedUserId,
            RejectionReason = r.RejectionReason
        }).ToList();

        // Build dictionary of valid (non-deleted) company IDs for company requests
        var validCompanyIds = new Dictionary<Guid, bool>();
        foreach (var request in companyRequestViewModels)
        {
            if (request.CreatedCompanyId.HasValue && !validCompanyIds.ContainsKey(request.CreatedCompanyId.Value))
            {
                var company = await _companyService.GetByIdAsync(request.CreatedCompanyId.Value);
                validCompanyIds[request.CreatedCompanyId.Value] = company != null && !company.IsDeleted;
            }
        }

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        ViewBag.CompanyRequests = companyRequestViewModels;
        ViewBag.ValidCompanyIds = validCompanyIds;

        return View();
    }

    /// <summary>
    /// Approve a company request from user signup.
    /// Note: This is for signup requests only. Admins create companies directly via CompanyCreate action.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CompanyRequestApprove(Guid id)
    {
        var request = await _companyRequestService.GetByIdAsync(id);
        if (request == null)
            return NotFound();

        var viewModel = new CompanyRequestApproveViewModel
        {
            Id = request.Id,
            CompanyName = request.CompanyName,
            ContactEmail = request.ContactEmail,
            PhoneNumber = request.PhoneNumber,
            Website = request.Website,
            Description = request.Description,
            RequesterFullName = request.RequesterFullName,
            RequesterEmail = request.RequesterEmail
        };

        return View(viewModel);
    }

    /// <summary>
    /// Approve a company request from user signup (POST).
    /// Note: This is for signup requests only. Admins create companies directly via CompanyCreate action.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompanyRequestApprove(CompanyRequestApproveViewModel viewModel)
    {
        try
        {
            var currentUser = await GetCurrentUserAsync();
            await _companyRequestService.ApproveRequestAsync(viewModel.Id, currentUser?.Email ?? CurrentUserEmail ?? "unknown");
            TempData["SuccessMessage"] = $"Company request approved. Company '{viewModel.CompanyName}' and user account for {viewModel.RequesterEmail} have been created.";
            return RedirectToAction(nameof(CompanyRequests));
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(CompanyRequestApprove), new { id = viewModel.Id });
        }
    }

    /// <summary>
    /// Reject a company request from user signup.
    /// Note: This is for signup requests only. Admins create companies directly via CompanyCreate action.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompanyRequestReject(Guid id, string? reason = null)
    {
        try
        {
            var currentUser = await GetCurrentUserAsync();
            await _companyRequestService.RejectRequestAsync(id, currentUser?.Email ?? CurrentUserEmail ?? "unknown", reason);
            TempData["SuccessMessage"] = "Company request rejected.";
            return RedirectToAction(nameof(CompanyRequests));
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(CompanyRequests));
        }
    }

    #region Company CRUD

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

    /// <summary>
    /// Direct company creation by admin (no request flow).
    /// This creates the company immediately without requiring approval.
    /// </summary>
    [HttpGet]
    public IActionResult CompanyCreate()
    {
        return View(new CompanyCreateViewModel());
    }

    /// <summary>
    /// Direct company creation by admin (POST) - no request flow.
    /// This creates the company immediately without requiring approval.
    /// Note: For user signup requests, use CompanyRequestApprove instead.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompanyCreate(CompanyCreateViewModel viewModel)
    {
        if (viewModel.CreateInitialAdmin)
        {
            if (string.IsNullOrWhiteSpace(viewModel.AdminFullName))
            {
                ModelState.AddModelError(nameof(viewModel.AdminFullName), "Admin full name is required when creating an initial admin.");
            }

            if (string.IsNullOrWhiteSpace(viewModel.AdminEmail))
            {
                ModelState.AddModelError(nameof(viewModel.AdminEmail), "Admin email is required when creating an initial admin.");
            }
        }

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
            if (viewModel.CreateInitialAdmin)
            {
                try
                {
                    await _userService.CreateUserAsync(
                        email: viewModel.AdminEmail!,
                        fullName: viewModel.AdminFullName!,
                        companyId: company.Id,
                        role: SAM.Domain.Enums.AppRoleEnum.company_admin,
                        generatePassword: true);
                }
                catch (Infrastructure.Exceptions.BusinessRuleException ex)
                {
                    TempData["WarningMessage"] = $"Company '{company.Name}' was created, but the initial company admin user could not be created: {ex.Message}";
                }
            }

            TempData["SuccessMessage"] = viewModel.CreateInitialAdmin
                ? $"Company '{company.Name}' and initial company admin user created successfully."
                : $"Company '{company.Name}' created successfully.";
            return RedirectToAction(nameof(Index), new { tab = "companies" });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(viewModel);
        }
    }

    [HttpGet]
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
            return RedirectToAction(nameof(Index), new { tab = "companies" });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(viewModel);
        }
    }

    [HttpGet]
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

        return RedirectToAction(nameof(Index), new { tab = "companies" });
    }

    #endregion
}

