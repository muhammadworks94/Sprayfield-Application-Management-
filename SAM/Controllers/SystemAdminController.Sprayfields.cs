using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Domain.Entities;
using SAM.Infrastructure.Authorization;
using SAM.Utilities;
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
        ViewBag.CanDuplicate = await IsGlobalAdminAsync() || await IsInRoleAsync("company_admin");

        var viewModel = new SprayfieldViewModel
        {
            Id = sprayfield.Id,
            CompanyId = sprayfield.CompanyId,
            CompanyName = sprayfield.Company?.Name,
            FieldId = sprayfield.FieldId,
            SizeAcres = sprayfield.SizeAcres,
            SoilName = SprayfieldZoneSummaryHelper.GetSoilSummary(sprayfield),
            CropName = SprayfieldZoneSummaryHelper.GetCropSummary(sprayfield),
            NozzleName = SprayfieldZoneSummaryHelper.GetNozzleSummary(sprayfield),
            FacilityId = sprayfield.FacilityId,
            FacilityName = sprayfield.Facility?.Name,
            HydraulicLoadingLimitInPerYr = sprayfield.HydraulicLoadingLimitInPerYr,
            HourlyRateInches = sprayfield.HourlyRateInches,
            WeeklyRateInches = sprayfield.WeeklyRateInches,
            Zones = sprayfield.ApplicationZones
                .OrderBy(z => z.ZoneName)
                .Select(z => new SprayfieldZoneDetailViewModel
                {
                    Id = z.Id,
                    ZoneName = z.ZoneName,
                    PercentOfField = z.PercentOfField,
                    Acres = z.Acres,
                    SoilName = z.Soil?.TypeName,
                    NozzleName = z.Nozzle is null ? null : $"{z.Nozzle.Manufacturer} {z.Nozzle.Model}",
                    CropName = z.Crop?.Name,
                    Active = z.Active
                })
                .ToList()
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
                    ZoneName = "A",
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
                viewModel.Zones.Add(new ApplicationZoneInputViewModel { ZoneName = "A", PercentOfField = 100m, Active = true });
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
            viewModel.Zones.Add(new ApplicationZoneInputViewModel { ZoneName = "A", PercentOfField = 100m, Active = true });
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
                    viewModel.Zones.Add(new ApplicationZoneInputViewModel { ZoneName = "A", PercentOfField = 100m, Active = true });
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
                ValidateRemovedZoneApplicationHandling(
                    viewModel.RemovedZoneAction,
                    viewModel.ReassignToZoneId,
                    viewModel.Zones,
                    removedZonesWithApplications,
                    nameof(viewModel.RemovedZoneAction),
                    nameof(viewModel.ReassignToZoneId));

                if (!ModelState.IsValid)
                {
                    await PopulateSprayfieldDropdownsAsync(viewModel.CompanyId);
                    return View(viewModel);
                }
            }

            sprayfield.FieldId = viewModel.FieldId;
            sprayfield.SizeAcres = viewModel.SizeAcres;
            sprayfield.FacilityId = viewModel.FacilityId;
            sprayfield.HydraulicLoadingLimitInPerYr = viewModel.HydraulicLoadingLimitInPerYr;
            sprayfield.HourlyRateInches = viewModel.HourlyRateInches;
            sprayfield.WeeklyRateInches = viewModel.WeeklyRateInches;

            await _sprayfieldService.UpdateAsync(sprayfield);
            if (removedZonesWithApplications.Count > 0)
            {
                await ApplyRemovedZoneApplicationsAsync(
                    viewModel.RemovedZoneAction,
                    viewModel.ReassignToZoneId,
                    viewModel.Zones,
                    removedZonesWithApplications);
            }
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

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> GetSprayfieldBulkEditZoneData([FromQuery] List<Guid> sprayfieldIds)
    {
        return await BuildSprayfieldBulkEditZoneDataAsync(sprayfieldIds);
    }

    [HttpPost]
    [ActionName("GetSprayfieldBulkEditZoneData")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> PostSprayfieldBulkEditZoneData([FromForm] List<Guid> sprayfieldIds)
    {
        return await BuildSprayfieldBulkEditZoneDataAsync(sprayfieldIds);
    }

    private async Task<IActionResult> BuildSprayfieldBulkEditZoneDataAsync(List<Guid> sprayfieldIds)
    {
        if (!await IsGlobalAdminAsync())
        {
            return Forbid();
        }

        var selectedIds = (sprayfieldIds ?? new List<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (selectedIds.Count == 0)
        {
            return BadRequest(new { message = "At least one sprayfield must be selected." });
        }

        var sprayfields = new List<Sprayfield>();
        Guid? companyId = null;

        foreach (var sprayfieldId in selectedIds)
        {
            var sprayfield = await _sprayfieldService.GetByIdAsync(sprayfieldId);
            if (sprayfield == null)
            {
                continue;
            }

            await EnsureCompanyAccessAsync(sprayfield.CompanyId);

            companyId ??= sprayfield.CompanyId;
            if (companyId.Value != sprayfield.CompanyId)
            {
                return BadRequest(new { message = "Bulk zone edit requires sprayfields from the same company." });
            }

            sprayfields.Add(sprayfield);
        }

        if (sprayfields.Count == 0 || !companyId.HasValue)
        {
            return BadRequest(new { message = "No sprayfields were found for bulk zone editing." });
        }

        var soils = (await _soilService.GetByCompanyIdAsync(companyId.Value))
            .OrderBy(s => s.TypeName)
            .Select(s => new { value = s.Id, text = s.TypeName });
        var crops = (await _cropService.GetByCompanyIdAsync(companyId.Value))
            .OrderBy(c => c.Name)
            .Select(c => new { value = c.Id, text = c.Name });
        var nozzles = (await _nozzleService.GetByCompanyIdAsync(companyId.Value))
            .OrderBy(n => n.Manufacturer)
            .ThenBy(n => n.Model)
            .Select(n => new { value = n.Id, text = $"{n.Manufacturer} {n.Model}" });

        var sprayfieldPayload = new List<object>();
        foreach (var sprayfield in sprayfields.OrderBy(s => BuildNaturalSortKey(s.FieldId)).ThenBy(s => s.FieldId))
        {
            var zones = (await _applicationZoneService.GetBySprayfieldIdAsync(sprayfield.Id)).ToList();
            sprayfieldPayload.Add(new
            {
                sprayfieldId = sprayfield.Id,
                fieldId = sprayfield.FieldId,
                zones = zones.Select(z => new
                {
                    id = z.Id,
                    zoneName = z.ZoneName,
                    percentOfField = z.PercentOfField,
                    soilId = z.SoilId,
                    nozzleId = z.NozzleId,
                    cropId = z.CropId,
                    active = z.Active
                }),
                reassignOptions = zones.Select(z => new
                {
                    value = z.Id,
                    text = z.ZoneName
                })
            });
        }

        return Json(new
        {
            sprayfields = sprayfieldPayload,
            soils,
            crops,
            nozzles
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> SprayfieldBulkEdit(SprayfieldBulkEditViewModel viewModel)
    {
        if (!await IsGlobalAdminAsync())
        {
            return Forbid();
        }

        var selectedIds = (viewModel.SelectedSprayfieldIds ?? new List<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (selectedIds.Count == 0)
        {
            TempData["ErrorMessage"] = "Please select at least one sprayfield for multi-edit.";
            return RedirectToAction("SystemAdmin", new { tab = "sprayfields" });
        }

        var hasAnyUpdate =
            viewModel.SizeAcres.HasValue ||
            viewModel.HourlyRateInches.HasValue ||
            viewModel.WeeklyRateInches.HasValue ||
            (viewModel.FacilityId.HasValue && viewModel.FacilityId.Value != Guid.Empty) ||
            (viewModel.ZoneEdits?.Any(z => z.ApplyZoneChanges) ?? false);

        if (!hasAnyUpdate)
        {
            return HandleBulkEditFailureAsync("Please choose at least one field to update.");
        }

        if (!ModelState.IsValid)
        {
            return HandleBulkEditFailureAsync("Please correct the highlighted bulk edit errors.");
        }

        var sprayfieldsToUpdate = new List<Sprayfield>();
        var zoneUpdateContexts = new List<BulkZoneUpdateContext>();

        foreach (var id in selectedIds)
        {
            var sprayfield = await _sprayfieldService.GetByIdAsync(id);
            if (sprayfield == null)
            {
                continue;
            }

            await EnsureCompanyAccessAsync(sprayfield.CompanyId);
            sprayfieldsToUpdate.Add(sprayfield);
        }

        for (var i = 0; i < (viewModel.ZoneEdits?.Count ?? 0); i++)
        {
            var zoneEdit = viewModel.ZoneEdits[i];
            if (!zoneEdit.ApplyZoneChanges)
            {
                continue;
            }

            if (zoneEdit.SprayfieldId == Guid.Empty || !selectedIds.Contains(zoneEdit.SprayfieldId))
            {
                ModelState.AddModelError($"ZoneEdits[{i}].SprayfieldId", "Invalid sprayfield selected for zone editing.");
                continue;
            }

            NormalizeZones(zoneEdit.Zones);

            foreach (var key in ModelState.Keys.Where(k => k == $"ZoneEdits[{i}].Zones" || k.StartsWith($"ZoneEdits[{i}].Zones[", StringComparison.Ordinal)).ToList())
            {
                ModelState.Remove(key);
            }

            ValidateZoneInputs(zoneEdit.Zones, $"ZoneEdits[{i}].Zones");
            if (!string.IsNullOrWhiteSpace(zoneEdit.RemovedZoneAction) &&
                !string.Equals(zoneEdit.RemovedZoneAction, "delete", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(zoneEdit.RemovedZoneAction, "reassign", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError($"ZoneEdits[{i}].RemovedZoneAction", "Removed zone action is invalid.");
            }

            var sprayfield = sprayfieldsToUpdate.FirstOrDefault(s => s.Id == zoneEdit.SprayfieldId);
            if (sprayfield == null)
            {
                ModelState.AddModelError($"ZoneEdits[{i}].SprayfieldId", "Sprayfield could not be loaded.");
                continue;
            }

            var existingZones = (await _applicationZoneService.GetBySprayfieldIdAsync(sprayfield.Id)).ToList();
            var incomingZoneIds = zoneEdit.Zones
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
                ValidateRemovedZoneApplicationHandling(
                    zoneEdit.RemovedZoneAction,
                    zoneEdit.ReassignToZoneId,
                    zoneEdit.Zones,
                    removedZonesWithApplications,
                    $"ZoneEdits[{i}].RemovedZoneAction",
                    $"ZoneEdits[{i}].ReassignToZoneId");
            }

            zoneUpdateContexts.Add(new BulkZoneUpdateContext
            {
                Sprayfield = sprayfield,
                ZoneEdit = zoneEdit,
                RemovedZonesWithApplications = removedZonesWithApplications
            });
        }

        if (!ModelState.IsValid)
        {
            return HandleBulkEditFailureAsync("Please correct the highlighted bulk edit errors.");
        }

        var updatedCount = 0;
        var zoneUpdatedCount = 0;
        foreach (var sprayfield in sprayfieldsToUpdate)
        {
            if (viewModel.SizeAcres.HasValue)
            {
                sprayfield.SizeAcres = viewModel.SizeAcres!.Value;
            }

            if (viewModel.HourlyRateInches.HasValue)
            {
                sprayfield.HourlyRateInches = viewModel.HourlyRateInches;
            }

            if (viewModel.WeeklyRateInches.HasValue)
            {
                sprayfield.WeeklyRateInches = viewModel.WeeklyRateInches;
            }

            // Facility is only updated when a specific facility value is provided.
            if (viewModel.FacilityId.HasValue && viewModel.FacilityId.Value != Guid.Empty)
            {
                sprayfield.FacilityId = viewModel.FacilityId.Value;
            }

            await _sprayfieldService.UpdateAsync(sprayfield);
            updatedCount++;
        }

        foreach (var zoneContext in zoneUpdateContexts)
        {
            if (zoneContext.RemovedZonesWithApplications.Count > 0)
            {
                await ApplyRemovedZoneApplicationsAsync(
                    zoneContext.ZoneEdit.RemovedZoneAction,
                    zoneContext.ZoneEdit.ReassignToZoneId,
                    zoneContext.ZoneEdit.Zones,
                    zoneContext.RemovedZonesWithApplications);
            }

            await SaveZonesAsync(zoneContext.Sprayfield.Id, zoneContext.Sprayfield.CompanyId, zoneContext.ZoneEdit.Zones);
            await _applicationZoneService.RecalculateForSprayfieldAsync(zoneContext.Sprayfield.Id);
            await _applicationZoneService.ValidatePercentTotalAsync(zoneContext.Sprayfield.Id);
            zoneUpdatedCount++;
        }

        var successMessage = updatedCount > 0 || zoneUpdatedCount > 0
            ? BuildBulkEditSuccessMessage(updatedCount, zoneUpdatedCount)
            : "No sprayfields were updated.";

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, message = successMessage });
        }

        TempData["SuccessMessage"] = successMessage;

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
            return RedirectToDuplicateSource(sprayfield.Id, sprayfield.FieldId);
        }

        try
        {
            var duplicate = new Sprayfield
            {
                CompanyId = sprayfield.CompanyId,
                FieldId = newFieldId.Trim(),
                SizeAcres = sprayfield.SizeAcres,
                FacilityId = sprayfield.FacilityId,
                HydraulicLoadingLimitInPerYr = sprayfield.HydraulicLoadingLimitInPerYr,
                HourlyRateInches = sprayfield.HourlyRateInches,
                WeeklyRateInches = sprayfield.WeeklyRateInches
            };

            await _sprayfieldService.CreateAsync(duplicate);

            var zoneCopies = sprayfield.ApplicationZones
                .OrderBy(z => z.ZoneName)
                .Select(z => new ApplicationZoneInputViewModel
                {
                    ZoneName = z.ZoneName,
                    PercentOfField = z.PercentOfField,
                    SoilId = z.SoilId,
                    NozzleId = z.NozzleId,
                    CropId = z.CropId,
                    Active = z.Active
                })
                .ToList();

            NormalizeZones(zoneCopies);
            await SaveZonesAsync(duplicate.Id, duplicate.CompanyId, zoneCopies);
            await _applicationZoneService.RecalculateForSprayfieldAsync(duplicate.Id);
            await _applicationZoneService.ValidatePercentTotalAsync(duplicate.Id);

            TempData["SuccessMessage"] = $"Sprayfield '{sprayfield.FieldId}' duplicated as '{duplicate.FieldId}'.";
            return RedirectToAction("SystemAdmin", new { tab = "sprayfields" });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToDuplicateSource(sprayfield.Id, sprayfield.FieldId);
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

    private void ValidateZoneInputs(List<ApplicationZoneInputViewModel> zones, string zonesKeyPrefix = "Zones")
    {
        if (zones.Count == 0)
        {
            ModelState.AddModelError(zonesKeyPrefix, "At least one zone is required.");
            return;
        }

        var activeZones = zones.Where(z => z.Active).ToList();
        if (activeZones.Count == 0)
        {
            ModelState.AddModelError(zonesKeyPrefix, "At least one active zone is required.");
            return;
        }

        for (var i = 0; i < zones.Count; i++)
        {
            var zone = zones[i];
            if (string.IsNullOrWhiteSpace(zone.ZoneName))
            {
                ModelState.AddModelError($"{zonesKeyPrefix}[{i}].ZoneName", "Zone name is required.");
            }

            if (zone.SoilId == Guid.Empty)
            {
                ModelState.AddModelError($"{zonesKeyPrefix}[{i}].SoilId", "Soil is required.");
            }

            if (zone.NozzleId == Guid.Empty)
            {
                ModelState.AddModelError($"{zonesKeyPrefix}[{i}].NozzleId", "Nozzle is required.");
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
            ModelState.AddModelError(zonesKeyPrefix, $"Duplicate zone names are not allowed: {string.Join(", ", duplicateNames)}.");
        }

        var totalPercent = activeZones.Sum(z => z.PercentOfField);
        if (Math.Abs(totalPercent - 100m) > 0.01m)
        {
            ModelState.AddModelError(zonesKeyPrefix, $"Active zone percentages must total 100%. Current total: {totalPercent:F2}%.");
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

    private void ValidateRemovedZoneApplicationHandling(
        string? removedZoneAction,
        Guid? reassignToZoneId,
        List<ApplicationZoneInputViewModel> zones,
        List<ApplicationZone> removedZonesWithApplications)
    {
        ValidateRemovedZoneApplicationHandling(
            removedZoneAction,
            reassignToZoneId,
            zones,
            removedZonesWithApplications,
            "RemovedZoneAction",
            "ReassignToZoneId");
    }

    private void ValidateRemovedZoneApplicationHandling(
        string? removedZoneAction,
        Guid? reassignToZoneId,
        List<ApplicationZoneInputViewModel> zones,
        List<ApplicationZone> removedZonesWithApplications,
        string removedZoneActionKey,
        string reassignToZoneIdKey)
    {
        var action = string.IsNullOrWhiteSpace(removedZoneAction) ? "delete" : removedZoneAction;

        if (string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!string.Equals(action, "reassign", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(removedZoneActionKey, "Select how removed zone applications should be handled.");
            return;
        }

        if (!reassignToZoneId.HasValue || reassignToZoneId.Value == Guid.Empty)
        {
            ModelState.AddModelError(reassignToZoneIdKey, "Select a target zone for reassignment.");
            return;
        }

        var removedZoneIds = removedZonesWithApplications.Select(z => z.Id).ToHashSet();
        var remainingZoneIds = zones
            .Where(z => z.Id.HasValue)
            .Select(z => z.Id!.Value)
            .ToHashSet();

        if (removedZoneIds.Contains(reassignToZoneId.Value) || !remainingZoneIds.Contains(reassignToZoneId.Value))
        {
            ModelState.AddModelError(reassignToZoneIdKey, "Select an existing remaining zone for reassignment.");
        }
    }

    private async Task ApplyRemovedZoneApplicationsAsync(
        string? removedZoneAction,
        Guid? reassignToZoneId,
        List<ApplicationZoneInputViewModel> zones,
        List<ApplicationZone> removedZonesWithApplications)
    {
        var action = string.IsNullOrWhiteSpace(removedZoneAction) ? "delete" : removedZoneAction;

        if (string.Equals(action, "reassign", StringComparison.OrdinalIgnoreCase) &&
            reassignToZoneId.HasValue &&
            reassignToZoneId.Value != Guid.Empty)
        {
            var removedZoneIds = removedZonesWithApplications.Select(z => z.Id).ToHashSet();
            var remainingZoneIds = zones
                .Where(z => z.Id.HasValue)
                .Select(z => z.Id!.Value)
                .ToHashSet();

            // Only reassign when target is valid and not being removed.
            if (!removedZoneIds.Contains(reassignToZoneId.Value) &&
                remainingZoneIds.Contains(reassignToZoneId.Value))
            {
                foreach (var removedZone in removedZonesWithApplications)
                {
                    await _monthlyApplicationService.ReassignZoneAsync(removedZone.Id, reassignToZoneId.Value);
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

    private IActionResult HandleBulkEditFailureAsync(string message)
    {
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return BadRequest(new
            {
                success = false,
                message,
                errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage).ToArray())
            });
        }

        TempData["ErrorMessage"] = message;
        return RedirectToAction("SystemAdmin", new { tab = "sprayfields" });
    }

    private IActionResult RedirectToDuplicateSource(Guid sprayfieldId, string sprayfieldFieldId)
    {
        TempData["DuplicateSprayfieldId"] = sprayfieldId.ToString();
        TempData["DuplicateSprayfieldFieldId"] = sprayfieldFieldId;
        return RedirectToAction("SystemAdmin", new { tab = "sprayfields" });
    }

    private static string BuildBulkEditSuccessMessage(int sprayfieldCount, int zonePanelCount)
    {
        var parts = new List<string>();
        if (sprayfieldCount > 0)
        {
            parts.Add($"updated {sprayfieldCount} sprayfield record(s)");
        }

        if (zonePanelCount > 0)
        {
            parts.Add($"saved zone changes for {zonePanelCount} sprayfield(s)");
        }

        return parts.Count > 0
            ? $"{char.ToUpperInvariant(parts[0][0])}{parts[0][1..]}{(parts.Count > 1 ? $" and {parts[1]}" : string.Empty)} successfully."
            : "No sprayfields were updated.";
    }

    private sealed class BulkZoneUpdateContext
    {
        public required Sprayfield Sprayfield { get; init; }

        public required SprayfieldBulkZoneEditViewModel ZoneEdit { get; init; }

        public List<ApplicationZone> RemovedZonesWithApplications { get; init; } = new();
    }
}



