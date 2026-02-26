using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
            WeeklyRateInches = sprayfield.WeeklyRateInches
        };

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> SprayfieldCreate(Guid? companyId = null)
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

        var viewModel = new SprayfieldCreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty
        };

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
                CropId = viewModel.CropId,
                NozzleId = viewModel.NozzleId,
                FacilityId = viewModel.FacilityId,
                HydraulicLoadingLimitInPerYr = viewModel.HydraulicLoadingLimitInPerYr,
                HourlyRateInches = viewModel.HourlyRateInches,
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
    public async Task<IActionResult> SprayfieldDelete(Guid id)
    {
        try
        {
            var Sprayfield = await _sprayfieldService.GetByIdAsync(id);
            if (Sprayfield != null)
            {
                await EnsureCompanyAccessAsync(Sprayfield.CompanyId);
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

        var sprayfield = await _sprayfieldService.GetByIdAsync(id);
        var companyId = sprayfield?.CompanyId;
        return RedirectToAction("SystemAdmin", new { tab = "sprayfields"});
    }

    #endregion

}



