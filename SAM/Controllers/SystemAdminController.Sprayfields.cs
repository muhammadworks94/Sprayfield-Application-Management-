using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAM.Domain.Entities;
using SAM.Infrastructure.Authorization;
using SAM.ViewModels.SystemAdmin;

namespace SAM.Controllers;

public partial class SystemAdminController
{
    #region Sprayfields

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> SprayfieldDetails(Guid id)
    {
        var sprayfield = await _sprayfieldService.GetByIdAsync(id);
        if (sprayfield == null) return NotFound();

        await EnsureCompanyAccessAsync(sprayfield.CompanyId);
        ViewBag.CanDuplicate = await IsGlobalAdminAsync() || await IsInRoleAsync("company_admin");

        var viewModel = new SprayfieldViewModel
        {
            Id = sprayfield.Id,
            CompanyId = sprayfield.CompanyId,
            CompanyName = sprayfield.Company?.Name,
            FieldId = sprayfield.FieldId,
            SizeAcres = sprayfield.SizeAcres,
            SoilName = sprayfield.Soil?.TypeName,
            CropName = sprayfield.Crop?.Name,
            NozzleName = sprayfield.Nozzle is null ? null : $"{sprayfield.Nozzle.Manufacturer} {sprayfield.Nozzle.Model}",
            FacilityId = sprayfield.FacilityId,
            FacilityName = sprayfield.Facility?.Name,
            HydraulicLoadingLimitInPerYr = sprayfield.HydraulicLoadingLimitInPerYr,
            HourlyRateInches = sprayfield.HourlyRateInches,
            AnnualRateInches = sprayfield.AnnualRateInches,
            WeeklyRateInches = sprayfield.WeeklyRateInches
        };

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> SprayfieldCreate(Guid? companyId = null)
    {
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();
        if (!companyId.HasValue && effectiveCompanyId.HasValue) companyId = effectiveCompanyId.Value;
        if (companyId.HasValue) await EnsureCompanyAccessAsync(companyId.Value);

        var viewModel = new SprayfieldCreateViewModel { CompanyId = companyId ?? Guid.Empty };
        await PopulateSprayfieldDropdownsAsync(viewModel.CompanyId);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
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
                NozzleId = viewModel.NozzleId,
                CropId = viewModel.CropId,
                FacilityId = viewModel.FacilityId,
                HydraulicLoadingLimitInPerYr = viewModel.HydraulicLoadingLimitInPerYr,
                HourlyRateInches = viewModel.HourlyRateInches,
                AnnualRateInches = viewModel.AnnualRateInches,
                WeeklyRateInches = viewModel.WeeklyRateInches
            };

            await _sprayfieldService.CreateAsync(sprayfield);
            TempData["SuccessMessage"] = $"Sprayfield '{sprayfield.FieldId}' created successfully.";
            return RedirectToAction("SystemAdmin", new { tab = "sprayfields" });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await PopulateSprayfieldDropdownsAsync(viewModel.CompanyId);
            return View(viewModel);
        }
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> SprayfieldEdit(Guid id)
    {
        var sprayfield = await _sprayfieldService.GetByIdAsync(id);
        if (sprayfield == null) return NotFound();

        await EnsureCompanyAccessAsync(sprayfield.CompanyId);

        var viewModel = new SprayfieldEditViewModel
        {
            Id = sprayfield.Id,
            CompanyId = sprayfield.CompanyId,
            FieldId = sprayfield.FieldId,
            SizeAcres = sprayfield.SizeAcres,
            SoilId = sprayfield.SoilId,
            NozzleId = sprayfield.NozzleId,
            CropId = sprayfield.CropId,
            FacilityId = sprayfield.FacilityId,
            HydraulicLoadingLimitInPerYr = sprayfield.HydraulicLoadingLimitInPerYr,
            HourlyRateInches = sprayfield.HourlyRateInches,
            AnnualRateInches = sprayfield.AnnualRateInches,
            WeeklyRateInches = sprayfield.WeeklyRateInches
        };

        await PopulateSprayfieldDropdownsAsync(sprayfield.CompanyId);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
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
            if (sprayfield == null) return NotFound();

            sprayfield.FieldId = viewModel.FieldId;
            sprayfield.SizeAcres = viewModel.SizeAcres;
            sprayfield.SoilId = viewModel.SoilId;
            sprayfield.NozzleId = viewModel.NozzleId;
            sprayfield.CropId = viewModel.CropId;
            sprayfield.FacilityId = viewModel.FacilityId;
            sprayfield.HydraulicLoadingLimitInPerYr = viewModel.HydraulicLoadingLimitInPerYr;
            sprayfield.HourlyRateInches = viewModel.HourlyRateInches;
            sprayfield.AnnualRateInches = viewModel.AnnualRateInches;
            sprayfield.WeeklyRateInches = viewModel.WeeklyRateInches;

            await _sprayfieldService.UpdateAsync(sprayfield);
            TempData["SuccessMessage"] = $"Sprayfield '{sprayfield.FieldId}' updated successfully.";
            return RedirectToAction("SystemAdmin", new { tab = "sprayfields" });
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
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> SprayfieldBulkEdit(SprayfieldBulkEditViewModel viewModel)
    {
        if (!await IsGlobalAdminAsync()) return Forbid();

        var selectedIds = (viewModel.SelectedSprayfieldIds ?? new List<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (selectedIds.Count == 0)
        {
            TempData["ErrorMessage"] = "Please select at least one sprayfield for multi-edit.";
            return RedirectToAction("SystemAdmin", new { tab = "sprayfields" });
        }

        foreach (var id in selectedIds)
        {
            var sprayfield = await _sprayfieldService.GetByIdAsync(id);
            if (sprayfield == null) continue;
            await EnsureCompanyAccessAsync(sprayfield.CompanyId);

            if (viewModel.SizeAcres.HasValue) sprayfield.SizeAcres = viewModel.SizeAcres.Value;
            if (viewModel.HourlyRateInches.HasValue) sprayfield.HourlyRateInches = viewModel.HourlyRateInches;
            if (viewModel.AnnualRateInches.HasValue) sprayfield.AnnualRateInches = viewModel.AnnualRateInches;
            if (viewModel.WeeklyRateInches.HasValue) sprayfield.WeeklyRateInches = viewModel.WeeklyRateInches;
            if (viewModel.FacilityId.HasValue && viewModel.FacilityId.Value != Guid.Empty) sprayfield.FacilityId = viewModel.FacilityId.Value;
            if (viewModel.SoilId.HasValue && viewModel.SoilId.Value != Guid.Empty) sprayfield.SoilId = viewModel.SoilId.Value;
            if (viewModel.NozzleId.HasValue && viewModel.NozzleId.Value != Guid.Empty) sprayfield.NozzleId = viewModel.NozzleId.Value;
            if (viewModel.CropId.HasValue) sprayfield.CropId = viewModel.CropId;

            await _sprayfieldService.UpdateAsync(sprayfield);
        }

        TempData["SuccessMessage"] = "Selected sprayfields updated successfully.";
        return RedirectToAction("SystemAdmin", new { tab = "sprayfields" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> SprayfieldDuplicate(Guid id, string newFieldId)
    {
        var sprayfield = await _sprayfieldService.GetByIdAsync(id);
        if (sprayfield == null)
        {
            TempData["ErrorMessage"] = "Sprayfield not found.";
            return RedirectToAction("SystemAdmin", new { tab = "sprayfields" });
        }

        await EnsureCompanyAccessAsync(sprayfield.CompanyId);

        if (string.IsNullOrWhiteSpace(newFieldId))
        {
            TempData["ErrorMessage"] = "A new Field ID is required to duplicate a sprayfield.";
            return RedirectToAction("SystemAdmin", new { tab = "sprayfields" });
        }

        try
        {
            var duplicate = new Sprayfield
            {
                CompanyId = sprayfield.CompanyId,
                FieldId = newFieldId.Trim(),
                SizeAcres = sprayfield.SizeAcres,
                SoilId = sprayfield.SoilId,
                NozzleId = sprayfield.NozzleId,
                CropId = sprayfield.CropId,
                FacilityId = sprayfield.FacilityId,
                HydraulicLoadingLimitInPerYr = sprayfield.HydraulicLoadingLimitInPerYr,
                HourlyRateInches = sprayfield.HourlyRateInches,
                AnnualRateInches = sprayfield.AnnualRateInches,
                WeeklyRateInches = sprayfield.WeeklyRateInches
            };

            await _sprayfieldService.CreateAsync(duplicate);
            TempData["SuccessMessage"] = $"Sprayfield '{sprayfield.FieldId}' duplicated as '{duplicate.FieldId}'.";
            return RedirectToAction("SystemAdmin", new { tab = "sprayfields" });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("SystemAdmin", new { tab = "sprayfields" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
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

        return RedirectToAction("SystemAdmin", new { tab = "sprayfields" });
    }

    #endregion
}
