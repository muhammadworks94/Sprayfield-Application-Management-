using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SAM.Domain.Entities;
using SAM.Infrastructure.Authorization;
using SAM.ViewModels.SystemAdmin;

namespace SAM.Controllers;

public partial class SystemAdminController
{
    #region Facilities

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> FacilityDetails(Guid id)
    {
        var facility = await _facilityService.GetByIdAsync(id);
        if (facility == null)
            return NotFound();

        // Check company access
        await EnsureCompanyAccessAsync(facility.CompanyId);

        var defaultPermit = facility.DefaultFacilityPermitId.HasValue
            ? await _context.FacilityPermits.AsNoTracking().FirstOrDefaultAsync(p => p.Id == facility.DefaultFacilityPermitId)
            : await _context.FacilityPermits.AsNoTracking()
                .Where(p => p.FacilityId == facility.Id && p.IsActive)
                .OrderByDescending(p => p.EffectiveStartDate)
                .FirstOrDefaultAsync();

        var viewModel = new FacilityViewModel
        {
            Id = facility.Id,
            CompanyId = facility.CompanyId,
            CompanyName = facility.Company?.Name,
            Name = facility.Name,
            Permittee = facility.Permittee,
            PermitNumber = defaultPermit?.PermitNumber ?? string.Empty,
            PermitExpirationDate = defaultPermit?.EffectiveEndDate,
            FacilityClass = facility.FacilityClass,
            PermitPhone = facility.PermitPhone,
            FacilityPhone = facility.FacilityPhone,
            FacilityContactPerson = facility.FacilityContactPerson,
            FacilityContactPersonPhone = facility.FacilityContactPersonPhone,
            SigningOfficial = facility.SigningOfficial,
            FacilityContactPersonTitle = facility.FacilityContactPersonTitle,
            OrcName = facility.OrcName,
            OperatorGrade = facility.OperatorGrade,
            OperatorNumber = facility.OperatorNumber,
            OperatorPhone = facility.OperatorPhone,
            ChangeInOrc = facility.ChangeInOrc ?? false,
            PersonsCollectingSamples = facility.PersonsCollectingSamples,
            MineralizationRatePercent = facility.MineralizationRatePercent,
            VolatilizationRatePercent = facility.VolatilizationRatePercent,
            DefaultFacilityPermitId = facility.DefaultFacilityPermitId ?? defaultPermit?.Id,
            SelectedPermitLabel = defaultPermit == null
                ? null
                : $"{defaultPermit.PermitNumber} v{defaultPermit.PermitVersion}",
            PermitLocationSummary = defaultPermit == null
                ? null
                : string.Join(", ", new[] { defaultPermit.Address, $"{defaultPermit.City}, {defaultPermit.State} {defaultPermit.ZipCode}".Trim(' ', ',') }
                    .Where(s => !string.IsNullOrWhiteSpace(s))),
            SelectedPermitCounty = defaultPermit?.County,
            PermitSprayfieldCount = defaultPermit?.TotalNumberOfSprayfields,
            PermitMinimumFreeboardFeet = defaultPermit?.PermittedMinimumFreeboardFeet,
            LagoonBermHeightFeet = facility.LagoonBermHeightFeet
        };

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> FacilityCreate(Guid? companyId = null)
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

        var viewModel = new FacilityCreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> FacilityCreate(FacilityCreateViewModel viewModel)
    {
        // Ensure company access
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }

        try
        {
            var facility = new Facility
            {
                CompanyId = viewModel.CompanyId,
                Name = viewModel.Name,
                Permittee = viewModel.Permittee,
                FacilityClass = viewModel.FacilityClass,
                PermitPhone = viewModel.PermitPhone,
                FacilityPhone = viewModel.FacilityPhone,
                FacilityContactPerson = viewModel.FacilityContactPerson,
                FacilityContactPersonPhone = viewModel.FacilityContactPersonPhone,
                SigningOfficial = viewModel.SigningOfficial,
                FacilityContactPersonTitle = viewModel.FacilityContactPersonTitle,
                OrcName = viewModel.OrcName,
                OperatorGrade = viewModel.OperatorGrade,
                OperatorNumber = viewModel.OperatorNumber,
                OperatorPhone = viewModel.OperatorPhone,
                ChangeInOrc = viewModel.ChangeInOrc,
                PersonsCollectingSamples = viewModel.PersonsCollectingSamples,
                MineralizationRatePercent = viewModel.MineralizationRatePercent ?? 40m,
                VolatilizationRatePercent = viewModel.VolatilizationRatePercent ?? 50m,
                LagoonBermHeightFeet = viewModel.LagoonBermHeightFeet
            };

            await _facilityService.CreateAsync(facility);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, id = facility.Id, name = facility.Name });
            }

            TempData["SuccessMessage"] = $"Facility '{facility.Name}' created successfully.";
            return RedirectToAction("SystemAdmin", new { tab = "facilities" });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Return partial form with validation errors for modal
                ViewBag.Companies = await GetCompanySelectListAsync();
                return PartialView("Partials/_FacilityCreateFormPartial", viewModel);
            }

            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> FacilityEdit(Guid id)
    {
        var facility = await _facilityService.GetByIdAsync(id);
        if (facility == null)
            return NotFound();

        // Check company access
        await EnsureCompanyAccessAsync(facility.CompanyId);

        var viewModel = new FacilityEditViewModel
        {
            Id = facility.Id,
            CompanyId = facility.CompanyId,
            Name = facility.Name,
            Permittee = facility.Permittee,
            FacilityClass = facility.FacilityClass,
            PermitPhone = facility.PermitPhone,
            FacilityPhone = facility.FacilityPhone,
            FacilityContactPerson = facility.FacilityContactPerson,
            FacilityContactPersonPhone = facility.FacilityContactPersonPhone,
            SigningOfficial = facility.SigningOfficial,
            FacilityContactPersonTitle = facility.FacilityContactPersonTitle,
            OrcName = facility.OrcName,
            OperatorGrade = facility.OperatorGrade,
            OperatorNumber = facility.OperatorNumber,
            OperatorPhone = facility.OperatorPhone,
            ChangeInOrc = facility.ChangeInOrc ?? false,
            PersonsCollectingSamples = facility.PersonsCollectingSamples,
            MineralizationRatePercent = facility.MineralizationRatePercent,
            VolatilizationRatePercent = facility.VolatilizationRatePercent,
            DefaultFacilityPermitId = facility.DefaultFacilityPermitId,
            LagoonBermHeightFeet = facility.LagoonBermHeightFeet
        };

        if (!viewModel.DefaultFacilityPermitId.HasValue)
        {
            viewModel.DefaultFacilityPermitId = await ResolveSingleFacilityPermitIdAsync(facility.Id);
        }

        ViewBag.Companies = await GetCompanySelectListAsync();
        ViewBag.FacilityPermits = await GetFacilityPermitSelectListAsync(facility.Id, viewModel.DefaultFacilityPermitId);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> FacilityEdit(FacilityEditViewModel viewModel)
    {
        // Ensure company access
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            ViewBag.FacilityPermits = await GetFacilityPermitSelectListAsync(viewModel.Id, viewModel.DefaultFacilityPermitId);
            return View(viewModel);
        }

        try
        {
            var facility = await _facilityService.GetByIdAsync(viewModel.Id);
            if (facility == null)
                return NotFound();

            viewModel.DefaultFacilityPermitId ??= await ResolveSingleFacilityPermitIdAsync(viewModel.Id);

            facility.Name = viewModel.Name;
            facility.Permittee = viewModel.Permittee;
            facility.FacilityClass = viewModel.FacilityClass;
            facility.PermitPhone = viewModel.PermitPhone;
            facility.FacilityPhone = viewModel.FacilityPhone;
            facility.FacilityContactPerson = viewModel.FacilityContactPerson;
            facility.FacilityContactPersonPhone = viewModel.FacilityContactPersonPhone;
            facility.SigningOfficial = viewModel.SigningOfficial;
            facility.FacilityContactPersonTitle = viewModel.FacilityContactPersonTitle;
            facility.OrcName = viewModel.OrcName;
            facility.OperatorGrade = viewModel.OperatorGrade;
            facility.OperatorNumber = viewModel.OperatorNumber;
            facility.OperatorPhone = viewModel.OperatorPhone;
            facility.ChangeInOrc = viewModel.ChangeInOrc;
            facility.PersonsCollectingSamples = viewModel.PersonsCollectingSamples;
            facility.MineralizationRatePercent = viewModel.MineralizationRatePercent;
            facility.VolatilizationRatePercent = viewModel.VolatilizationRatePercent;
            facility.DefaultFacilityPermitId = viewModel.DefaultFacilityPermitId;
            facility.LagoonBermHeightFeet = viewModel.LagoonBermHeightFeet;

            await _facilityService.UpdateAsync(facility);
            TempData["SuccessMessage"] = $"Facility '{facility.Name}' updated successfully.";
            return RedirectToAction("SystemAdmin", new { tab = "facilities" });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();
            ViewBag.FacilityPermits = await GetFacilityPermitSelectListAsync(viewModel.Id, viewModel.DefaultFacilityPermitId);
            return View(viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> FacilityDelete(Guid id)
    {
        try
        {
            var Facility = await _facilityService.GetByIdAsync(id);
            if (Facility != null)
            {
                await EnsureCompanyAccessAsync(Facility.CompanyId);
            }

            await _facilityService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Facility deleted successfully.";
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "Facility not found.";
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        var facility = await _facilityService.GetByIdAsync(id);
        var companyId = facility?.CompanyId;
        return RedirectToAction("SystemAdmin", new { tab = "facilities"});
    }

    #endregion

}



