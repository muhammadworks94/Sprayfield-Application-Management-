using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAM.Infrastructure.Authorization;
using SAM.ViewModels.Common;
using SAM.ViewModels.UserManagement;

namespace SAM.Controllers;

public partial class UserManagementController
{
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
    public async Task<IActionResult> UserRequestApprove(Guid id)
    {
        var request = await _userRequestService.GetByIdAsync(id);
        if (request == null)
            return NotFound();

        if (request.CompanyId.HasValue)
        {
            await EnsureCompanyAccessAsync(request.CompanyId.Value);
        }
        else if (!await IsGlobalAdminAsync())
        {
            return Forbid();
        }

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
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> UserRequestApprove(UserRequestApproveViewModel viewModel)
    {
        var request = await _userRequestService.GetByIdAsync(viewModel.Id);
        if (request == null)
            return NotFound();

        if (request.CompanyId.HasValue)
        {
            await EnsureCompanyAccessAsync(request.CompanyId.Value);
        }
        else if (!await IsGlobalAdminAsync())
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            // Reload request role context on validation failure.
            viewModel.RequestedRole = request.AppRole;
            ViewBag.Roles = GetAppRoleSelectList();
            return View(viewModel);
        }

        try
        {
            var currentUser = await GetCurrentUserAsync();
            await _userRequestService.ApproveRequestAsync(viewModel.Id, currentUser?.Email ?? CurrentUserEmail ?? "unknown", viewModel.AppRole);
            TempData["SuccessMessage"] = $"User request approved. {viewModel.AppRole} account created for {viewModel.FullName}.";
            return RedirectToAction(nameof(Index), new { tab = "requests" });
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
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> UserRequestReject(Guid id, string? reason = null)
    {
        try
        {
            var request = await _userRequestService.GetByIdAsync(id);
            if (request == null)
                return NotFound();

            if (request.CompanyId.HasValue)
            {
                await EnsureCompanyAccessAsync(request.CompanyId.Value);
            }
            else if (!await IsGlobalAdminAsync())
            {
                return Forbid();
            }

            var currentUser = await GetCurrentUserAsync();
            await _userRequestService.RejectRequestAsync(id, currentUser?.Email ?? CurrentUserEmail ?? "unknown", reason);
            TempData["SuccessMessage"] = "User request rejected.";
            return RedirectToAction(nameof(Index), new { tab = "requests" });
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

}
