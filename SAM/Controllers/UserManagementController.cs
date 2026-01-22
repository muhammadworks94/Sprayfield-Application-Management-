using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Controllers.Base;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Authorization;
using SAM.Services.Interfaces;
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

    public UserManagementController(
        IUserRequestService userRequestService,
        IAdminRequestService adminRequestService,
        ICompanyService companyService,
        UserManager<ApplicationUser> userManager,
        ILogger<UserManagementController> logger)
        : base(userManager, logger)
    {
        _userRequestService = userRequestService;
        _adminRequestService = adminRequestService;
        _companyService = companyService;
    }

    #region User Requests

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> UserRequests(Guid? companyId = null)
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

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        ViewBag.Companies = await GetCompanySelectListAsync();
        ViewBag.SelectedCompanyId = companyId;

        return View(viewModels);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> UserRequestCreate(Guid? companyId = null)
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
            RequestedByEmail = request.RequestedByEmail
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> UserRequestApprove(UserRequestApproveViewModel viewModel)
    {
        try
        {
            var currentUser = await GetCurrentUserAsync();
            await _userRequestService.ApproveRequestAsync(viewModel.Id, currentUser?.Email ?? CurrentUserEmail ?? "unknown");
            TempData["SuccessMessage"] = $"User request approved. User account created for {viewModel.Email}.";
            return RedirectToAction(nameof(UserRequests));
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
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

    private SelectList GetAppRoleSelectList()
    {
        // Exclude admin role from company admin requests
        var roles = Enum.GetValues(typeof(AppRoleEnum))
            .Cast<AppRoleEnum>()
            .Where(r => r != AppRoleEnum.admin)
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

