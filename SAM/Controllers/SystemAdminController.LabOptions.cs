using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAM.Domain.Entities;
using SAM.Infrastructure.Authorization;
using SAM.Services.Interfaces;
using SAM.ViewModels.SystemAdmin;

namespace SAM.Controllers;

public partial class SystemAdminController
{
    private ICompanyLabOptionService CompanyLabOptionService =>
        HttpContext.RequestServices.GetRequiredService<ICompanyLabOptionService>();

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> LabOptionCreate(Guid? companyId = null)
    {
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();
        if (!companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        if (companyId.HasValue)
        {
            await EnsureCompanyAccessAsync(companyId.Value);
        }

        var viewModel = new LabOptionCreateViewModel { CompanyId = companyId ?? Guid.Empty };
        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> LabOptionCreate(LabOptionCreateViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);
        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }

        var entity = new CompanyLabOption
        {
            CompanyId = viewModel.CompanyId,
            Name = viewModel.Name.Trim(),
            CertificationNumber = viewModel.CertificationNumber?.Trim() ?? string.Empty,
            IsActive = true
        };
        await CompanyLabOptionService.CreateAsync(entity);
        TempData["SuccessMessage"] = $"Lab option '{entity.Name}' created successfully.";
        return RedirectToAction(nameof(SystemAdmin), new { tab = "laboptions" });
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> LabOptionEdit(Guid id)
    {
        var labOption = await CompanyLabOptionService.GetByIdAsync(id);
        if (labOption == null)
        {
            return NotFound();
        }

        await EnsureCompanyAccessAsync(labOption.CompanyId);
        var viewModel = new LabOptionEditViewModel
        {
            Id = labOption.Id,
            CompanyId = labOption.CompanyId,
            Name = labOption.Name,
            CertificationNumber = labOption.CertificationNumber,
            SortOrder = labOption.SortOrder,
            IsActive = labOption.IsActive
        };
        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> LabOptionEdit(LabOptionEditViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);
        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }

        var entity = new CompanyLabOption
        {
            Id = viewModel.Id,
            CompanyId = viewModel.CompanyId,
            Name = viewModel.Name.Trim(),
            CertificationNumber = viewModel.CertificationNumber?.Trim() ?? string.Empty,
            SortOrder = viewModel.SortOrder,
            IsActive = viewModel.IsActive
        };
        await CompanyLabOptionService.UpdateAsync(entity);
        TempData["SuccessMessage"] = $"Lab option '{entity.Name}' updated successfully.";
        return RedirectToAction(nameof(SystemAdmin), new { tab = "laboptions" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> LabOptionDelete(Guid id)
    {
        var labOption = await CompanyLabOptionService.GetByIdAsync(id);
        if (labOption == null)
        {
            return NotFound();
        }

        await EnsureCompanyAccessAsync(labOption.CompanyId);
        await CompanyLabOptionService.DeleteAsync(id);
        TempData["SuccessMessage"] = "Lab option deleted successfully.";
        return RedirectToAction(nameof(SystemAdmin), new { tab = "laboptions" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> LabOptionReorder(Guid companyId, string orderedIds)
    {
        await EnsureCompanyAccessAsync(companyId);
        var ids = orderedIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Guid.Parse)
            .ToList();
        await CompanyLabOptionService.ReorderAsync(companyId, ids);
        TempData["SuccessMessage"] = "Lab option order updated.";
        return RedirectToAction(nameof(SystemAdmin), new { tab = "laboptions" });
    }
}
