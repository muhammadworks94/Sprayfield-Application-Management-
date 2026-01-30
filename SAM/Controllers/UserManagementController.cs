using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Controllers.Base;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Authorization;
using SAM.Services.Interfaces;
using SAM.ViewModels.Common;
using SAM.ViewModels.UserManagement;

namespace SAM.Controllers;

/// <summary>
/// Controller for User Management module - handling user and admin requests.
/// </summary>
[Authorize]
public class UserManagementController : BaseController
{
    private readonly IUserRequestService _userRequestService;
    private readonly IAdminRequestService _adminRequestService;
    private readonly ICompanyService _companyService;
    private readonly ICompanyRequestService _companyRequestService;
    private readonly IUserService _userService;

    public UserManagementController(
        IUserRequestService userRequestService,
        IAdminRequestService adminRequestService,
        ICompanyService companyService,
        ICompanyRequestService companyRequestService,
        IUserService userService,
        UserManager<ApplicationUser> userManager,
        ILogger<UserManagementController> logger)
        : base(userManager, logger)
    {
        _userRequestService = userRequestService;
        _adminRequestService = adminRequestService;
        _companyService = companyService;
        _companyRequestService = companyRequestService;
        _userService = userService;
    }

    #region User Requests

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> UserRequests(Guid? companyId = null)
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

        var requests = await _userRequestService.GetAllAsync(companyId);
        
        var viewModels = requests.Select(r => new UserRequestViewModel
        {
            Id = r.Id,
            CompanyId = r.CompanyId,
            CompanyName = r.CompanyName,
            FullName = r.FullName,
            Email = r.Email,
            AppRole = r.AppRole,
            RequestedByEmail = r.RequestedByEmail,
            Status = r.Status,
            CreatedDate = r.CreatedDate
        });

        // Create filter view model
        var filterViewModel = new FilterViewModel
        {
            PageName = "UserRequests",
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
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> UserRequestCreate(Guid? companyId = null)
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

        var viewModel = new UserRequestCreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        ViewBag.Roles = GetAppRoleSelectList();

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> UserRequestCreate(UserRequestCreateViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            ViewBag.Roles = GetAppRoleSelectList();
            return View(viewModel);
        }

        try
        {
            var company = await _companyService.GetByIdAsync(viewModel.CompanyId);
            if (company == null)
            {
                ModelState.AddModelError("CompanyId", "Company not found.");
                ViewBag.Companies = await GetCompanySelectListAsync();
                ViewBag.Roles = GetAppRoleSelectList();
                return View(viewModel);
            }

            var currentUser = await GetCurrentUserAsync();
            var userRequest = new UserRequest
            {
                CompanyId = viewModel.CompanyId,
                CompanyName = company.Name,
                FullName = viewModel.FullName,
                Email = viewModel.Email,
                AppRole = viewModel.AppRole,
                RequestedByEmail = currentUser?.Email ?? CurrentUserEmail ?? "unknown",
                Status = RequestStatusEnum.Pending
            };

            await _userRequestService.CreateAsync(userRequest);
            TempData["SuccessMessage"] = $"User request created successfully. The request will be reviewed by administrators.";
            return RedirectToAction(nameof(UserRequests), new { companyId = viewModel.CompanyId });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();
            ViewBag.Roles = GetAppRoleSelectList();
            return View(viewModel);
        }
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> UserRequestApprove(Guid id)
    {
        var request = await _userRequestService.GetByIdAsync(id);
        if (request == null)
            return NotFound();

        var viewModel = new UserRequestApproveViewModel
        {
            Id = request.Id,
            FullName = request.FullName,
            Email = request.Email,
            CompanyName = request.CompanyName,
            AppRole = request.AppRole,
            RequestedRole = request.AppRole,
            RequestedByEmail = request.RequestedByEmail
        };

        ViewBag.Roles = GetAppRoleSelectList();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> UserRequestApprove(UserRequestApproveViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            // Reload request to get original requested role
            var request = await _userRequestService.GetByIdAsync(viewModel.Id);
            if (request != null)
            {
                viewModel.RequestedRole = request.AppRole;
            }
            ViewBag.Roles = GetAppRoleSelectList();
            return View(viewModel);
        }

        try
        {
            var currentUser = await GetCurrentUserAsync();
            await _userRequestService.ApproveRequestAsync(viewModel.Id, currentUser?.Email ?? CurrentUserEmail ?? "unknown", viewModel.AppRole);
            TempData["SuccessMessage"] = $"User request approved. {viewModel.AppRole} account created for {viewModel.FullName}.";
            return RedirectToAction(nameof(UserRequests));
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            ViewBag.Roles = GetAppRoleSelectList();
            return RedirectToAction(nameof(UserRequestApprove), new { id = viewModel.Id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> UserRequestReject(Guid id, string? reason = null)
    {
        try
        {
            var currentUser = await GetCurrentUserAsync();
            await _userRequestService.RejectRequestAsync(id, currentUser?.Email ?? CurrentUserEmail ?? "unknown", reason);
            TempData["SuccessMessage"] = "User request rejected.";
            return RedirectToAction(nameof(UserRequests));
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(UserRequests));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> UserRequestDelete(Guid id)
    {
        try
        {
            await _userRequestService.SoftDeleteAsync(id);
            TempData["SuccessMessage"] = "User request deleted.";
            return RedirectToAction(nameof(UserRequests));
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "User request not found.";
            return RedirectToAction(nameof(UserRequests));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred while deleting the user request: {ex.Message}";
            return RedirectToAction(nameof(UserRequests));
        }
    }

    #endregion

    #region Admin Requests

    [HttpGet]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> AdminRequests()
    {
        var requests = await _adminRequestService.GetAllAsync();
        
        var viewModels = requests.Select(r => new AdminRequestViewModel
        {
            Id = r.Id,
            RequestType = r.RequestType,
            TargetEmail = r.TargetEmail,
            TargetFullName = r.TargetFullName,
            Justification = r.Justification,
            RequestedByEmail = r.RequestedByEmail,
            Status = r.Status,
            CreatedDate = r.CreatedDate
        });

        return View(viewModels);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireAdmin)]
    public IActionResult AdminRequestCreate()
    {
        var viewModel = new AdminRequestCreateViewModel();
        ViewBag.RequestTypes = GetAdminRequestTypeSelectList();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> AdminRequestCreate(AdminRequestCreateViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.RequestTypes = GetAdminRequestTypeSelectList();
            return View(viewModel);
        }

        try
        {
            var currentUser = await GetCurrentUserAsync();
            var adminRequest = new AdminRequest
            {
                RequestType = viewModel.RequestType,
                TargetEmail = viewModel.TargetEmail,
                TargetFullName = viewModel.TargetFullName,
                Justification = viewModel.Justification,
                RequestedByEmail = currentUser?.Email ?? CurrentUserEmail ?? "unknown",
                Status = RequestStatusEnum.Pending
            };

            await _adminRequestService.CreateAsync(adminRequest);
            TempData["SuccessMessage"] = "Admin request created successfully. The request will be reviewed by other administrators.";
            return RedirectToAction(nameof(AdminRequests));
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.RequestTypes = GetAdminRequestTypeSelectList();
            return View(viewModel);
        }
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> AdminRequestApprove(Guid id)
    {
        var request = await _adminRequestService.GetByIdAsync(id);
        if (request == null)
            return NotFound();

        var viewModel = new AdminRequestApproveViewModel
        {
            Id = request.Id,
            RequestType = request.RequestType,
            TargetEmail = request.TargetEmail,
            TargetFullName = request.TargetFullName,
            Justification = request.Justification,
            RequestedByEmail = request.RequestedByEmail
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> AdminRequestApprove(AdminRequestApproveViewModel viewModel)
    {
        try
        {
            var currentUser = await GetCurrentUserAsync();
            await _adminRequestService.ApproveRequestAsync(viewModel.Id, currentUser?.Email ?? CurrentUserEmail ?? "unknown");
            
            var action = viewModel.RequestType == AdminRequestTypeEnum.CreateAdmin ? "created" : "disabled";
            TempData["SuccessMessage"] = $"Admin request approved. User account {action} for {viewModel.TargetEmail}.";
            return RedirectToAction(nameof(AdminRequests));
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(AdminRequestApprove), new { id = viewModel.Id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> AdminRequestReject(Guid id, string? reason = null)
    {
        try
        {
            var currentUser = await GetCurrentUserAsync();
            await _adminRequestService.RejectRequestAsync(id, currentUser?.Email ?? CurrentUserEmail ?? "unknown", reason);
            TempData["SuccessMessage"] = "Admin request rejected.";
            return RedirectToAction(nameof(AdminRequests));
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(AdminRequests));
        }
    }

    #endregion

    #region Company Requests

    [HttpGet]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> CompanyRequests()
    {
        var requests = await _companyRequestService.GetAllAsync();
        
        var viewModels = requests.Select(r => new CompanyRequestViewModel
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
        });

        return View(viewModels);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireAdmin)]
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
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

    #endregion

    #region Users

    [HttpGet]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Users(Guid? companyId = null)
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

        // Get users based on company filter
        IEnumerable<ApplicationUser> users;
        if (companyId.HasValue)
        {
            users = await _userService.GetUsersByCompanyAsync(companyId.Value);
        }
        else if (isGlobalAdmin)
        {
            users = await _userService.GetAllUsersAsync();
        }
        else
        {
            users = await _userService.GetUsersByCompanyAsync(effectiveCompanyId);
        }

        // Map to view models with roles
        var viewModels = new List<UserViewModel>();
        foreach (var user in users)
        {
            var roles = await UserManager.GetRolesAsync(user);
            viewModels.Add(new UserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                CompanyId = user.CompanyId,
                CompanyName = user.Company?.Name,
                Roles = roles.ToList(),
                IsActive = user.IsActive
            });
        }

        // Create filter view model
        var filterViewModel = new FilterViewModel
        {
            PageName = "Users",
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
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> UserDetails(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();

        if (user.CompanyId.HasValue)
        {
            await EnsureCompanyAccessAsync(user.CompanyId.Value);
        }

        var roles = await UserManager.GetRolesAsync(user);
        var viewModel = new UserDetailsViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            CompanyId = user.CompanyId,
            CompanyName = user.Company?.Name,
            Roles = roles.ToList(),
            IsActive = user.IsActive,
            UserName = user.UserName ?? string.Empty,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            CreatedDate = null, // ApplicationUser doesn't have CreatedDate (it's IdentityUser)
            UpdatedDate = null // ApplicationUser doesn't have UpdatedDate (it's IdentityUser)
        };

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> UserCreate(Guid? companyId = null)
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

        var viewModel = new UserCreateViewModel
        {
            CompanyId = companyId
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        ViewBag.Roles = GetAppRoleSelectList(includeAdmin: true);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> UserCreate(UserCreateViewModel viewModel)
    {
        if (viewModel.CompanyId.HasValue)
        {
            await EnsureCompanyAccessAsync(viewModel.CompanyId.Value);
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            ViewBag.Roles = GetAppRoleSelectList(includeAdmin: true);
            return View(viewModel);
        }

        try
        {
            var user = await _userService.CreateUserAsync(
                viewModel.Email,
                viewModel.FullName,
                viewModel.CompanyId,
                viewModel.AppRole,
                generatePassword: true);

            TempData["SuccessMessage"] = $"User '{viewModel.FullName}' created successfully. A temporary password has been generated.";
            return RedirectToAction(nameof(UserDetails), new { id = user.Id });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();
            ViewBag.Roles = GetAppRoleSelectList(includeAdmin: true);
            return View(viewModel);
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();
            ViewBag.Roles = GetAppRoleSelectList(includeAdmin: true);
            return View(viewModel);
        }
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> UserEdit(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();

        if (user.CompanyId.HasValue)
        {
            await EnsureCompanyAccessAsync(user.CompanyId.Value);
        }

        var roles = await UserManager.GetRolesAsync(user);
        var currentRole = AppRoleEnum.@operator; // Default
        if (roles.Any())
        {
            var roleName = roles.First();
            currentRole = roleName switch
            {
                "admin" => AppRoleEnum.admin,
                "company_admin" => AppRoleEnum.company_admin,
                "operator" => AppRoleEnum.@operator,
                "technician" => AppRoleEnum.technician,
                _ => AppRoleEnum.@operator
            };
        }

        var viewModel = new UserEditViewModel
        {
            Id = user.Id,
            CompanyId = user.CompanyId,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            AppRole = currentRole,
            IsActive = user.IsActive
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        ViewBag.Roles = GetAppRoleSelectList(includeAdmin: true);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> UserEdit(UserEditViewModel viewModel)
    {
        if (viewModel.CompanyId.HasValue)
        {
            await EnsureCompanyAccessAsync(viewModel.CompanyId.Value);
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            ViewBag.Roles = GetAppRoleSelectList(includeAdmin: true);
            return View(viewModel);
        }

        try
        {
            var user = await _userService.UpdateUserAsync(
                viewModel.Id,
                viewModel.FullName,
                viewModel.Email,
                viewModel.CompanyId,
                viewModel.AppRole,
                viewModel.IsActive);

            TempData["SuccessMessage"] = $"User '{viewModel.FullName}' updated successfully.";
            return RedirectToAction(nameof(UserDetails), new { id = user.Id });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();
            ViewBag.Roles = GetAppRoleSelectList(includeAdmin: true);
            return View(viewModel);
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();
            ViewBag.Roles = GetAppRoleSelectList(includeAdmin: true);
            return View(viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> UserDelete(string id)
    {
        ApplicationUser? user = null;

        try
        {
            user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            if (user.CompanyId.HasValue)
            {
                await EnsureCompanyAccessAsync(user.CompanyId.Value);
            }

            await _userService.DeleteUserAsync(id);
            TempData["SuccessMessage"] = $"User '{user.FullName}' deleted successfully.";
            return RedirectToAction(nameof(Users), new { companyId = user.CompanyId });
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "User not found.";
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Users), new { companyId = user?.CompanyId });
    }

    #endregion

    #region Helper Methods

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

    private SelectList GetAppRoleSelectList(bool includeAdmin = false)
    {
        // Exclude admin role by default (for company admin requests)
        // Include admin role when managing users (admin-only page)
        var roles = Enum.GetValues(typeof(AppRoleEnum))
            .Cast<AppRoleEnum>()
            .Where(r => includeAdmin || r != AppRoleEnum.admin)
            .Select(e => new SelectListItem
            {
                Value = e.ToString(),
                Text = e.ToString().Replace("_", " ").Replace("operator", "Operator")
            })
            .ToList();

        return new SelectList(roles, "Value", "Text");
    }

    private SelectList GetAdminRequestTypeSelectList()
    {
        return new SelectList(Enum.GetValues(typeof(AdminRequestTypeEnum)).Cast<AdminRequestTypeEnum>()
            .Select(e => new SelectListItem
            {
                Value = e.ToString(),
                Text = e.ToString().Replace("Admin", " Admin")
            }), "Value", "Text");
    }

    #endregion
}


