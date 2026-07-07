using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Infrastructure.Exceptions;
using SAM.ViewModels.CompanyManagement;

namespace SAM.Controllers;

public partial class CompanyManagementController
{
    [HttpGet]
    public async Task<IActionResult> ClientSetup(Guid? facilityId = null, int? throughYear = null, int? throughMonth = null)
    {
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();
        var defaultThrough = DateTime.UtcNow.AddMonths(-1);
        var resolvedThroughYear = throughYear ?? defaultThrough.Year;
        var resolvedThroughMonth = throughMonth ?? defaultThrough.Month;

        ViewBag.EffectiveCompanyId = effectiveCompanyId;
        ViewBag.Facilities = await GetFacilitySelectListAsync(effectiveCompanyId, facilityId);
        ViewBag.Months = GetMonthSelectList(resolvedThroughMonth);

        if (!facilityId.HasValue || facilityId.Value == Guid.Empty)
        {
            return View(new ClientSetupViewModel
            {
                CompanyId = effectiveCompanyId ?? Guid.Empty,
                ThroughYear = resolvedThroughYear,
                ThroughMonth = resolvedThroughMonth
            });
        }

        var facility = await _facilityService.GetByIdAsync(facilityId.Value);
        if (facility == null)
        {
            return NotFound();
        }

        await EnsureCompanyAccessAsync(facility.CompanyId);

        if (effectiveCompanyId.HasValue && facility.CompanyId != effectiveCompanyId.Value)
        {
            TempData["ErrorMessage"] = "Selected facility does not belong to the company selected in the header.";
            return RedirectToAction(nameof(ClientSetup), new { throughYear = resolvedThroughYear, throughMonth = resolvedThroughMonth });
        }

        var viewModel = await _baselineMonthlyLoadingService.GetSetupGridAsync(
            facilityId.Value,
            resolvedThroughYear,
            resolvedThroughMonth);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClientSetup(ClientSetupViewModel viewModel, string submitAction)
    {
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        ViewBag.EffectiveCompanyId = effectiveCompanyId;
        ViewBag.Facilities = await GetFacilitySelectListAsync(
            effectiveCompanyId,
            viewModel.FacilityId == Guid.Empty ? null : viewModel.FacilityId);
        ViewBag.Months = GetMonthSelectList(viewModel.ThroughMonth);

        if (viewModel.FacilityId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(viewModel.FacilityId), "Facility is required.");
            return View(viewModel);
        }

        var facility = await _facilityService.GetByIdAsync(viewModel.FacilityId);
        if (facility == null)
        {
            ModelState.AddModelError(nameof(viewModel.FacilityId), "Facility not found.");
            return View(viewModel);
        }

        await EnsureCompanyAccessAsync(facility.CompanyId);

        if (effectiveCompanyId.HasValue && facility.CompanyId != effectiveCompanyId.Value)
        {
            ModelState.AddModelError(nameof(viewModel.FacilityId), "Selected facility does not belong to the company selected in the header.");
            return View(viewModel);
        }

        viewModel.CompanyId = facility.CompanyId;

        if (!ModelState.IsValid)
        {
            if (viewModel.MonthRows.Count == 0)
            {
                viewModel = await _baselineMonthlyLoadingService.GetSetupGridAsync(
                    viewModel.FacilityId,
                    viewModel.ThroughYear,
                    viewModel.ThroughMonth);
            }

            return View(viewModel);
        }

        try
        {
            var cells = viewModel.MonthRows
                .SelectMany(row => row.Cells.Select(cell => new ClientSetupCellSaveRequest
                {
                    SprayfieldId = cell.SprayfieldId,
                    Year = row.Year,
                    Month = row.Month,
                    LoadingInches = cell.LoadingInches
                }))
                .ToList();

            await _baselineMonthlyLoadingService.SaveSetupGridAsync(
                viewModel.FacilityId,
                viewModel.ThroughYear,
                viewModel.ThroughMonth,
                cells,
                CurrentUserEmail ?? "system");

            viewModel.RefreshNdarAfterSave = string.Equals(submitAction, "saveAndRefresh", StringComparison.OrdinalIgnoreCase);

            if (viewModel.RefreshNdarAfterSave)
            {
                await _baselineMonthlyLoadingService.RefreshNdarReportsForWindowAsync(
                    viewModel.FacilityId,
                    viewModel.ThroughYear,
                    viewModel.ThroughMonth);
                TempData["SuccessMessage"] = "Baseline monthly loading saved and NDAR reports refreshed for the 12-month window.";
            }
            else
            {
                TempData["SuccessMessage"] = "Baseline monthly loading saved successfully.";
            }
        }
        catch (EntityNotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return View(viewModel);
        }
        catch (BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return View(viewModel);
        }

        return RedirectToAction(nameof(ClientSetup), new
        {
            facilityId = viewModel.FacilityId,
            throughYear = viewModel.ThroughYear,
            throughMonth = viewModel.ThroughMonth
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetFacilitiesForClientSetup()
    {
        var companyId = await GetEffectiveCompanyIdAsync();
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
        {
            return Json(Array.Empty<object>());
        }

        await EnsureCompanyAccessAsync(companyId.Value);

        var facilities = await _facilityService.GetByCompanyIdAsync(companyId.Value);
        return Json(facilities
            .OrderBy(f => f.Name)
            .Select(f => new { id = f.Id, name = f.Name }));
    }

    private async Task<SelectList> GetFacilitySelectListAsync(Guid? companyId, Guid? selectedFacilityId = null)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
        {
            return new SelectList(Enumerable.Empty<SelectListItem>());
        }

        var facilities = await _facilityService.GetByCompanyIdAsync(companyId.Value);
        return new SelectList(facilities.OrderBy(f => f.Name), "Id", "Name", selectedFacilityId);
    }

    private static SelectList GetMonthSelectList(int? selectedMonth = null)
    {
        var months = Enumerable.Range(1, 12)
            .Select(m => new SelectListItem
            {
                Value = m.ToString(),
                Text = new DateTime(2000, m, 1).ToString("MMMM")
            });

        return new SelectList(months, "Value", "Text", selectedMonth);
    }
}
