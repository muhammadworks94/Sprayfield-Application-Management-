using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Domain.Entities;
using SAM.Infrastructure.Authorization;
using SAM.ViewModels.SystemAdmin;

namespace SAM.Controllers;

public partial class SystemAdminController
{
    #region Crops

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> CropCreate(Guid? companyId = null)
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

        var viewModel = new CropCreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
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
                NUptake = viewModel.NUptake
            };

            await _cropService.CreateAsync(crop);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, id = crop.Id, name = crop.Name });
            }

            TempData["SuccessMessage"] = $"Crop '{crop.Name}' created successfully.";
            return RedirectToAction("SystemAdmin", new { tab = "crops" });
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
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
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
            NUptake = crop.NUptake
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
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

            crop.CompanyId = viewModel.CompanyId;
            crop.Name = viewModel.Name;
            crop.NUptake = viewModel.NUptake;

            await _cropService.UpdateAsync(crop);
            TempData["SuccessMessage"] = $"Crop '{crop.Name}' updated successfully.";
            return RedirectToAction("SystemAdmin", new { tab = "crops" });
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
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> CropDelete(Guid id)
    {
        try
        {
            var Crop = await _cropService.GetByIdAsync(id);
            if (Crop != null)
            {
                await EnsureCompanyAccessAsync(Crop.CompanyId);
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

        var crop = await _cropService.GetByIdAsync(id);
        var companyId = crop?.CompanyId;
        return RedirectToAction("SystemAdmin", new { tab = "crops"});
    }

    #endregion

}



