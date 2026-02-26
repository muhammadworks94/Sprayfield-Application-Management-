using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        var viewModel = new FacilityViewModel
        {
            Id = facility.Id,
            CompanyId = facility.CompanyId,
            CompanyName = facility.Company?.Name,
            Name = facility.Name,
            PermitNumber = facility.PermitNumber,
            Permittee = facility.Permittee,
            FacilityClass = facility.FacilityClass,
            Address = facility.Address,
            City = facility.City,
            State = facility.State,
            ZipCode = facility.ZipCode,
            County = facility.County,
            PermitExpirationDate = facility.PermitExpirationDate,
            PermitPhone = facility.PermitPhone,
            FacilityPhone = facility.FacilityPhone,
            OrcName = facility.OrcName,
            OperatorGrade = facility.OperatorGrade,
            OperatorNumber = facility.OperatorNumber,
            ChangeInOrc = facility.ChangeInOrc,
            TotalNumberOfSprayfields = facility.TotalNumberOfSprayfields,
            CertifiedLaboratory1Name = facility.CertifiedLaboratory1Name,
            CertifiedLaboratory2Name = facility.CertifiedLaboratory2Name,
            LabCertificationNumber1 = facility.LabCertificationNumber1,
            LabCertificationNumber2 = facility.LabCertificationNumber2,
            PersonsCollectingSamples = facility.PersonsCollectingSamples,
            PermittedMinimumFreeboardFeet = facility.PermittedMinimumFreeboardFeet,
            MineralizationRatePercent = facility.MineralizationRatePercent,
            VolatilizationRatePercent = facility.VolatilizationRatePercent
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
                PermitNumber = viewModel.PermitNumber,
                Permittee = viewModel.Permittee,
                FacilityClass = viewModel.FacilityClass,
                Address = viewModel.Address,
                City = viewModel.City,
                State = viewModel.State,
                ZipCode = viewModel.ZipCode,
                County = viewModel.County,
                PermitExpirationDate = viewModel.PermitExpirationDate,
                PermitPhone = viewModel.PermitPhone,
                FacilityPhone = viewModel.FacilityPhone,
                OrcName = viewModel.OrcName,
                OperatorGrade = viewModel.OperatorGrade,
                OperatorNumber = viewModel.OperatorNumber,
                ChangeInOrc = viewModel.ChangeInOrc,
                TotalNumberOfSprayfields = viewModel.TotalNumberOfSprayfields,
                CertifiedLaboratory1Name = viewModel.CertifiedLaboratory1Name,
                CertifiedLaboratory2Name = viewModel.CertifiedLaboratory2Name,
                LabCertificationNumber1 = viewModel.LabCertificationNumber1,
                LabCertificationNumber2 = viewModel.LabCertificationNumber2,
                PersonsCollectingSamples = viewModel.PersonsCollectingSamples,
                PermittedMinimumFreeboardFeet = viewModel.PermittedMinimumFreeboardFeet,
                MineralizationRatePercent = viewModel.MineralizationRatePercent ?? 40m,
                VolatilizationRatePercent = viewModel.VolatilizationRatePercent ?? 50m
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
            PermitNumber = facility.PermitNumber,
            Permittee = facility.Permittee,
            FacilityClass = facility.FacilityClass,
            Address = facility.Address,
            City = facility.City,
            State = facility.State,
            ZipCode = facility.ZipCode,
            County = facility.County,
            PermitExpirationDate = facility.PermitExpirationDate,
            PermitPhone = facility.PermitPhone,
            FacilityPhone = facility.FacilityPhone,
            OrcName = facility.OrcName,
            OperatorGrade = facility.OperatorGrade,
            OperatorNumber = facility.OperatorNumber,
            ChangeInOrc = facility.ChangeInOrc,
            TotalNumberOfSprayfields = facility.TotalNumberOfSprayfields,
            CertifiedLaboratory1Name = facility.CertifiedLaboratory1Name,
            CertifiedLaboratory2Name = facility.CertifiedLaboratory2Name,
            LabCertificationNumber1 = facility.LabCertificationNumber1,
            LabCertificationNumber2 = facility.LabCertificationNumber2,
            PersonsCollectingSamples = facility.PersonsCollectingSamples,
            PermittedMinimumFreeboardFeet = facility.PermittedMinimumFreeboardFeet,
            MineralizationRatePercent = facility.MineralizationRatePercent,
            VolatilizationRatePercent = facility.VolatilizationRatePercent
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
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
            return View(viewModel);
        }

        try
        {
            var facility = await _facilityService.GetByIdAsync(viewModel.Id);
            if (facility == null)
                return NotFound();

            facility.Name = viewModel.Name;
            facility.PermitNumber = viewModel.PermitNumber;
            facility.Permittee = viewModel.Permittee;
            facility.FacilityClass = viewModel.FacilityClass;
            facility.Address = viewModel.Address;
            facility.City = viewModel.City;
            facility.State = viewModel.State;
            facility.ZipCode = viewModel.ZipCode;
            facility.County = viewModel.County;
            facility.PermitExpirationDate = viewModel.PermitExpirationDate;
            facility.PermitPhone = viewModel.PermitPhone;
            facility.FacilityPhone = viewModel.FacilityPhone;
            facility.OrcName = viewModel.OrcName;
            facility.OperatorGrade = viewModel.OperatorGrade;
            facility.OperatorNumber = viewModel.OperatorNumber;
            facility.ChangeInOrc = viewModel.ChangeInOrc;
            facility.TotalNumberOfSprayfields = viewModel.TotalNumberOfSprayfields;
            facility.CertifiedLaboratory1Name = viewModel.CertifiedLaboratory1Name;
            facility.CertifiedLaboratory2Name = viewModel.CertifiedLaboratory2Name;
            facility.LabCertificationNumber1 = viewModel.LabCertificationNumber1;
            facility.LabCertificationNumber2 = viewModel.LabCertificationNumber2;
            facility.PersonsCollectingSamples = viewModel.PersonsCollectingSamples;
            facility.PermittedMinimumFreeboardFeet = viewModel.PermittedMinimumFreeboardFeet;
            facility.MineralizationRatePercent = viewModel.MineralizationRatePercent;
            facility.VolatilizationRatePercent = viewModel.VolatilizationRatePercent;

            await _facilityService.UpdateAsync(facility);
            TempData["SuccessMessage"] = $"Facility '{facility.Name}' updated successfully.";
            return RedirectToAction("SystemAdmin", new { tab = "facilities" });
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



