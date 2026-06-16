using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Domain.Entities;
using SAM.Infrastructure.Authorization;
using SAM.ViewModels.SystemAdmin;

namespace SAM.Controllers;

public partial class SystemAdminController
{
    #region Monitoring Wells

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> MonitoringWellDetails(Guid id)
    {
        var monitoringWell = await _monitoringWellService.GetByIdAsync(id);
        if (monitoringWell == null)
            return NotFound();

        await EnsureCompanyAccessAsync(monitoringWell.CompanyId);

        var viewModel = new MonitoringWellViewModel
        {
            Id = monitoringWell.Id,
            CompanyId = monitoringWell.CompanyId,
            CompanyName = monitoringWell.Company?.Name,
            WellId = monitoringWell.WellId,
            WellPermitNumber = monitoringWell.WellPermitNumber,
            LocationDescription = monitoringWell.LocationDescription,
            DiameterInches = monitoringWell.DiameterInches,
            WellDepthFeet = monitoringWell.WellDepthFeet,
            DepthToScreenFeet = monitoringWell.DepthToScreenFeet,
            LowScreenDepthFeet = monitoringWell.LowScreenDepthFeet,
            HighScreenDepthFeet = monitoringWell.HighScreenDepthFeet,
            ScreenedIntervalFromFeet = monitoringWell.ScreenedIntervalFromFeet,
            ScreenedIntervalToFeet = monitoringWell.ScreenedIntervalToFeet,
            MeasuringPointAboveLandSurface = monitoringWell.MeasuringPointAboveLandSurface,
            RelativeMpElevation = monitoringWell.RelativeMpElevation,
            TopOfCasingElevationMsl = monitoringWell.TopOfCasingElevationMsl,
            TreatmentSystemLocation = monitoringWell.TreatmentSystemLocation,
            NumberOfWellsToBeSampled = monitoringWell.NumberOfWellsToBeSampled,
            Latitude = monitoringWell.Latitude,
            Longitude = monitoringWell.Longitude
        };

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> MonitoringWellCreate(Guid? companyId = null)
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

        var viewModel = new MonitoringWellCreateViewModel
        {
            CompanyId = companyId ?? Guid.Empty
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> MonitoringWellCreate(MonitoringWellCreateViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }

        try
        {
            var monitoringWell = new MonitoringWell
            {
                CompanyId = viewModel.CompanyId,
                WellId = viewModel.WellId,
                WellPermitNumber = viewModel.WellPermitNumber,
                LocationDescription = viewModel.LocationDescription,
                DiameterInches = viewModel.DiameterInches,
                WellDepthFeet = viewModel.WellDepthFeet,
                DepthToScreenFeet = viewModel.DepthToScreenFeet,
                LowScreenDepthFeet = viewModel.LowScreenDepthFeet,
                HighScreenDepthFeet = viewModel.HighScreenDepthFeet,
                ScreenedIntervalFromFeet = viewModel.ScreenedIntervalFromFeet,
                ScreenedIntervalToFeet = viewModel.ScreenedIntervalToFeet,
                MeasuringPointAboveLandSurface = viewModel.MeasuringPointAboveLandSurface,
                RelativeMpElevation = viewModel.RelativeMpElevation,
                TopOfCasingElevationMsl = viewModel.TopOfCasingElevationMsl,
                TreatmentSystemLocation = viewModel.TreatmentSystemLocation,
                NumberOfWellsToBeSampled = viewModel.NumberOfWellsToBeSampled,
                Latitude = viewModel.Latitude,
                Longitude = viewModel.Longitude
            };

            await _monitoringWellService.CreateAsync(monitoringWell);
            TempData["SuccessMessage"] = $"Monitoring well '{monitoringWell.WellId}' created successfully.";
            return RedirectToAction("SystemAdmin", new { tab = "monitoringwells" });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> MonitoringWellEdit(Guid id)
    {
        var monitoringWell = await _monitoringWellService.GetByIdAsync(id);
        if (monitoringWell == null)
            return NotFound();

        await EnsureCompanyAccessAsync(monitoringWell.CompanyId);

        var viewModel = new MonitoringWellEditViewModel
        {
            Id = monitoringWell.Id,
            CompanyId = monitoringWell.CompanyId,
            WellId = monitoringWell.WellId,
            WellPermitNumber = monitoringWell.WellPermitNumber,
            LocationDescription = monitoringWell.LocationDescription,
            DiameterInches = monitoringWell.DiameterInches,
            WellDepthFeet = monitoringWell.WellDepthFeet,
            DepthToScreenFeet = monitoringWell.DepthToScreenFeet,
            LowScreenDepthFeet = monitoringWell.LowScreenDepthFeet,
            HighScreenDepthFeet = monitoringWell.HighScreenDepthFeet,
            ScreenedIntervalFromFeet = monitoringWell.ScreenedIntervalFromFeet,
            ScreenedIntervalToFeet = monitoringWell.ScreenedIntervalToFeet,
            MeasuringPointAboveLandSurface = monitoringWell.MeasuringPointAboveLandSurface,
            RelativeMpElevation = monitoringWell.RelativeMpElevation,
            TopOfCasingElevationMsl = monitoringWell.TopOfCasingElevationMsl,
            TreatmentSystemLocation = monitoringWell.TreatmentSystemLocation,
            NumberOfWellsToBeSampled = monitoringWell.NumberOfWellsToBeSampled,
            Latitude = monitoringWell.Latitude,
            Longitude = monitoringWell.Longitude
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> MonitoringWellEdit(MonitoringWellEditViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            return View(viewModel);
        }

        try
        {
            var monitoringWell = await _monitoringWellService.GetByIdAsync(viewModel.Id);
            if (monitoringWell == null)
                return NotFound();

            monitoringWell.WellId = viewModel.WellId;
            monitoringWell.WellPermitNumber = viewModel.WellPermitNumber;
            monitoringWell.LocationDescription = viewModel.LocationDescription;
            monitoringWell.DiameterInches = viewModel.DiameterInches;
            monitoringWell.WellDepthFeet = viewModel.WellDepthFeet;
            monitoringWell.DepthToScreenFeet = viewModel.DepthToScreenFeet;
            monitoringWell.LowScreenDepthFeet = viewModel.LowScreenDepthFeet;
            monitoringWell.HighScreenDepthFeet = viewModel.HighScreenDepthFeet;
            monitoringWell.ScreenedIntervalFromFeet = viewModel.ScreenedIntervalFromFeet;
            monitoringWell.ScreenedIntervalToFeet = viewModel.ScreenedIntervalToFeet;
            monitoringWell.MeasuringPointAboveLandSurface = viewModel.MeasuringPointAboveLandSurface;
            monitoringWell.RelativeMpElevation = viewModel.RelativeMpElevation;
            monitoringWell.TopOfCasingElevationMsl = viewModel.TopOfCasingElevationMsl;
            monitoringWell.TreatmentSystemLocation = viewModel.TreatmentSystemLocation;
            monitoringWell.NumberOfWellsToBeSampled = viewModel.NumberOfWellsToBeSampled;
            monitoringWell.Latitude = viewModel.Latitude;
            monitoringWell.Longitude = viewModel.Longitude;

            await _monitoringWellService.UpdateAsync(monitoringWell);
            TempData["SuccessMessage"] = $"Monitoring well '{monitoringWell.WellId}' updated successfully.";
            return RedirectToAction("SystemAdmin", new { tab = "monitoringwells" });
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
    public async Task<IActionResult> MonitoringWellDelete(Guid id)
    {
        try
        {
            var MonitoringWell = await _monitoringWellService.GetByIdAsync(id);
            if (MonitoringWell != null)
            {
                await EnsureCompanyAccessAsync(MonitoringWell.CompanyId);
            }

            await _monitoringWellService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Monitoring well deleted successfully.";
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "Monitoring well not found.";
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        var monitoringWell = await _monitoringWellService.GetByIdAsync(id);
        var companyId = monitoringWell?.CompanyId;
        return RedirectToAction("SystemAdmin", new { tab = "monitoringwells"});
    }

    #endregion

}



