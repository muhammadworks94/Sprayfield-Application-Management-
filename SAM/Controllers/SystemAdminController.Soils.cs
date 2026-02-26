using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Domain.Entities;
using SAM.Infrastructure.Authorization;
using SAM.ViewModels.SystemAdmin;

namespace SAM.Controllers;

public partial class SystemAdminController
{
    #region Soils

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> SoilCreate(Guid? companyId = null)
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

        var viewModel = new SoilCreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
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
            return RedirectToAction("SystemAdmin", new { tab = "soils" });
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
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
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
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
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
            return RedirectToAction("SystemAdmin", new { tab = "soils" });
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
    public async Task<IActionResult> SoilDelete(Guid id)
    {
        try
        {
            var Soil = await _soilService.GetByIdAsync(id);
            if (Soil != null)
            {
                await EnsureCompanyAccessAsync(Soil.CompanyId);
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

        var soil = await _soilService.GetByIdAsync(id);
        var companyId = soil?.CompanyId;
        return RedirectToAction("SystemAdmin", new { tab = "soils" });
    }

    #endregion

}



