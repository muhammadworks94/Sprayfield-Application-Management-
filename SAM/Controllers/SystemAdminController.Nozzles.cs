using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Domain.Entities;
using SAM.Infrastructure.Authorization;
using SAM.ViewModels.SystemAdmin;

namespace SAM.Controllers;

public partial class SystemAdminController
{
    #region Nozzles

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> NozzleCreate(Guid? companyId = null)
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

        var viewModel = new NozzleCreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
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
                SprayArc = viewModel.SprayArc,
                Comment = viewModel.Comment
            };

            await _nozzleService.CreateAsync(nozzle);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var label = $"{nozzle.Manufacturer} {nozzle.Model}";
                return Json(new { success = true, id = nozzle.Id, name = label });
            }

            TempData["SuccessMessage"] = $"Nozzle '{nozzle.Manufacturer} {nozzle.Model}' created successfully.";
            return RedirectToAction("SystemAdmin", new { tab = "nozzles" });
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
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
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
            SprayArc = nozzle.SprayArc,
            Comment = nozzle.Comment
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
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
            nozzle.Comment = viewModel.Comment;

            await _nozzleService.UpdateAsync(nozzle);
            TempData["SuccessMessage"] = $"Nozzle '{nozzle.Manufacturer} {nozzle.Model}' updated successfully.";
            return RedirectToAction("SystemAdmin", new { tab = "nozzles" });
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
    public async Task<IActionResult> NozzleDelete(Guid id)
    {
        try
        {
            var Nozzle = await _nozzleService.GetByIdAsync(id);
            if (Nozzle != null)
            {
                await EnsureCompanyAccessAsync(Nozzle.CompanyId);
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

        var nozzle = await _nozzleService.GetByIdAsync(id);
        var companyId = nozzle?.CompanyId;
        return RedirectToAction("SystemAdmin", new { tab = "nozzles" });
    }

    #endregion

}



