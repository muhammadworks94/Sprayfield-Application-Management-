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
            CompanyId = companyId ?? Guid.Empty,
            Zones = new List<ApplicationZoneInputViewModel>
            {
                new()
                {
                    ZoneName = "Default",
                    PercentOfField = 100m,
                    Active = true
                }
            }
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
        NormalizeZones(viewModel.Zones);
        ValidateZoneInputs(viewModel.Zones);

        if (!ModelState.IsValid)
        {
            if (viewModel.Zones.Count == 0)
            {
                viewModel.Zones.Add(new ApplicationZoneInputViewModel { Active = true });
            }
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
            await SaveZonesAsync(sprayfield.Id, sprayfield.CompanyId, viewModel.Zones);
            await _applicationZoneService.ValidatePercentTotalAsync(sprayfield.Id);
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
            WeeklyRateInches = sprayfield.WeeklyRateInches,
            RemovedZoneAction = "delete",
            Zones = (await _applicationZoneService.GetBySprayfieldIdAsync(sprayfield.Id))
                .Select(z => new ApplicationZoneInputViewModel
                {
                    Id = z.Id,
                    ZoneName = z.ZoneName,
                    PercentOfField = z.PercentOfField,
                    SoilId = z.SoilId,
                    NozzleId = z.NozzleId,
                    CropId = z.CropId,
                    Active = z.Active
                })
                .ToList()
        };

        if (viewModel.Zones.Count == 0)
        {
            viewModel.Zones.Add(new ApplicationZoneInputViewModel { Active = true });
        }

        await PopulateSprayfieldDropdownsAsync(sprayfield.CompanyId);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> SprayfieldEdit(SprayfieldEditViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);
        NormalizeZones(viewModel.Zones);

        // Row deletions can leave transient binder errors for removed collection indices.
        // Zone validation is handled explicitly below, so clear auto-generated zone modelstate entries first.
        foreach (var key in ModelState.Keys.Where(k => k == "Zones" || k.StartsWith("Zones[", StringComparison.Ordinal)).ToList())
        {
            ModelState.Remove(key);
        }

        ValidateZoneInputs(viewModel.Zones);

        if (!ModelState.IsValid)
        {
            if (viewModel.Zones.Count == 0)
            {
                viewModel.Zones.Add(new ApplicationZoneInputViewModel { Active = true });
            }
            await PopulateSprayfieldDropdownsAsync(viewModel.CompanyId);
            return View(viewModel);
        }

        try
        {
            var sprayfield = await _sprayfieldService.GetByIdAsync(viewModel.Id);
            if (sprayfield == null)
                return NotFound();

            var existingZones = (await _applicationZoneService.GetBySprayfieldIdAsync(sprayfield.Id)).ToList();
            var incomingZoneIds = viewModel.Zones
                .Where(z => z.Id.HasValue)
                .Select(z => z.Id!.Value)
                .ToHashSet();
            var removedZones = existingZones
                .Where(z => !incomingZoneIds.Contains(z.Id))
                .ToList();

            var removedZonesWithApplications = new List<ApplicationZone>();
            foreach (var removedZone in removedZones)
            {
                if (await _monthlyApplicationService.AnyByZoneIdAsync(removedZone.Id))
                {
                    removedZonesWithApplications.Add(removedZone);
                }
            }

            if (removedZonesWithApplications.Count > 0)
            {
                await HandleRemovedZoneApplicationsAsync(viewModel, removedZonesWithApplications);
                if (!ModelState.IsValid)
                {
                    await PopulateSprayfieldDropdownsAsync(viewModel.CompanyId);
                    return View(viewModel);
                }
            }

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
            await SaveZonesAsync(sprayfield.Id, sprayfield.CompanyId, viewModel.Zones);
            await _applicationZoneService.RecalculateForSprayfieldAsync(sprayfield.Id);
            await _applicationZoneService.ValidatePercentTotalAsync(sprayfield.Id);
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

    private static void NormalizeZones(List<ApplicationZoneInputViewModel> zones)
    {
        zones.RemoveAll(z => string.IsNullOrWhiteSpace(z.ZoneName) && z.PercentOfField <= 0 && z.SoilId == Guid.Empty && z.NozzleId == Guid.Empty && !z.CropId.HasValue);
    }

    private void ValidateZoneInputs(List<ApplicationZoneInputViewModel> zones)
    {
        if (zones.Count == 0)
        {
            ModelState.AddModelError("Zones", "At least one zone is required.");
            return;
        }

        var activeZones = zones.Where(z => z.Active).ToList();
        if (activeZones.Count == 0)
        {
            ModelState.AddModelError("Zones", "At least one active zone is required.");
            return;
        }

        for (var i = 0; i < zones.Count; i++)
        {
            var zone = zones[i];
            if (string.IsNullOrWhiteSpace(zone.ZoneName))
            {
                ModelState.AddModelError($"Zones[{i}].ZoneName", "Zone name is required.");
            }

            if (zone.SoilId == Guid.Empty)
            {
                ModelState.AddModelError($"Zones[{i}].SoilId", "Soil is required.");
            }

            if (zone.NozzleId == Guid.Empty)
            {
                ModelState.AddModelError($"Zones[{i}].NozzleId", "Nozzle is required.");
            }
        }

        var duplicateNames = zones
            .Where(z => !string.IsNullOrWhiteSpace(z.ZoneName))
            .GroupBy(z => z.ZoneName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateNames.Count > 0)
        {
            ModelState.AddModelError("Zones", $"Duplicate zone names are not allowed: {string.Join(", ", duplicateNames)}.");
        }

        var totalPercent = activeZones.Sum(z => z.PercentOfField);
        if (Math.Abs(totalPercent - 100m) > 0.01m)
        {
            ModelState.AddModelError("Zones", $"Active zone percentages must total 100%. Current total: {totalPercent:F2}%.");
        }
    }

    private async Task SaveZonesAsync(Guid sprayfieldId, Guid companyId, List<ApplicationZoneInputViewModel> zones)
    {
        var existingZones = (await _applicationZoneService.GetBySprayfieldIdAsync(sprayfieldId)).ToList();
        var incomingIds = zones.Where(z => z.Id.HasValue).Select(z => z.Id!.Value).ToHashSet();

        foreach (var existing in existingZones.Where(z => !incomingIds.Contains(z.Id)))
        {
            await _applicationZoneService.DeleteAsync(existing.Id);
        }

        foreach (var zoneVm in zones)
        {
            if (zoneVm.Id.HasValue)
            {
                var zone = new ApplicationZone
                {
                    Id = zoneVm.Id.Value,
                    CompanyId = companyId,
                    SprayfieldId = sprayfieldId,
                    ZoneName = zoneVm.ZoneName.Trim(),
                    PercentOfField = zoneVm.PercentOfField,
                    SoilId = zoneVm.SoilId,
                    NozzleId = zoneVm.NozzleId,
                    CropId = zoneVm.CropId,
                    Active = zoneVm.Active
                };
                await _applicationZoneService.UpdateAsync(zone);
            }
            else
            {
                var zone = new ApplicationZone
                {
                    CompanyId = companyId,
                    SprayfieldId = sprayfieldId,
                    ZoneName = zoneVm.ZoneName.Trim(),
                    PercentOfField = zoneVm.PercentOfField,
                    SoilId = zoneVm.SoilId,
                    NozzleId = zoneVm.NozzleId,
                    CropId = zoneVm.CropId,
                    Active = zoneVm.Active
                };
                await _applicationZoneService.CreateAsync(zone);
            }
        }
    }

    private async Task HandleRemovedZoneApplicationsAsync(
        SprayfieldEditViewModel viewModel,
        List<ApplicationZone> removedZonesWithApplications)
    {
        var action = string.IsNullOrWhiteSpace(viewModel.RemovedZoneAction) ? "delete" : viewModel.RemovedZoneAction;

        if (string.Equals(action, "reassign", StringComparison.OrdinalIgnoreCase) &&
            viewModel.ReassignToZoneId.HasValue &&
            viewModel.ReassignToZoneId.Value != Guid.Empty)
        {
            var removedZoneIds = removedZonesWithApplications.Select(z => z.Id).ToHashSet();
            var remainingZoneIds = viewModel.Zones
                .Where(z => z.Id.HasValue)
                .Select(z => z.Id!.Value)
                .ToHashSet();

            // Only reassign when target is valid and not being removed.
            if (!removedZoneIds.Contains(viewModel.ReassignToZoneId.Value) &&
                remainingZoneIds.Contains(viewModel.ReassignToZoneId.Value))
            {
                foreach (var removedZone in removedZonesWithApplications)
                {
                    await _monthlyApplicationService.ReassignZoneAsync(removedZone.Id, viewModel.ReassignToZoneId.Value);
                }
                return;
            }
        }

        // Default/fallback behavior: delete dependent monthly applications so update succeeds in one submit.
        foreach (var removedZone in removedZonesWithApplications)
        {
            await _monthlyApplicationService.DeleteByZoneIdAsync(removedZone.Id);
        }
    }
}



