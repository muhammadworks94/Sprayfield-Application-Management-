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

        await EnsureCompanyAccessAsync(facility.CompanyId);

        var defaultPermit = facility.DefaultFacilityPermitId.HasValue
            ? await _context.FacilityPermits.AsNoTracking().FirstOrDefaultAsync(p => p.Id == facility.DefaultFacilityPermitId)
            : await _context.FacilityPermits.AsNoTracking()
                .Where(p => p.FacilityId == facility.Id && p.IsActive)
                .OrderByDescending(p => p.EffectiveStartDate)
                .FirstOrDefaultAsync();

        var currentOrc = await _facilityOrcAssignmentService.GetCurrentAsync(facility.Id);
        var history = await _facilityOrcAssignmentService.GetByFacilityIdAsync(facility.Id);

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
            CurrentOrcStartDate = currentOrc?.StartDate,
            CurrentOrcUserName = currentOrc?.User?.FullName
                ?? (string.IsNullOrWhiteSpace(currentOrc?.User?.Email) ? null : currentOrc.User.Email),
            OrcAssignmentHistory = history.Select(MapOrcAssignmentItem).ToList(),
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
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

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
        ViewBag.CompanyUsers = await GetCompanyUserSelectListAsync(viewModel.CompanyId);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> FacilityCreate(FacilityCreateViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            ViewBag.CompanyUsers = await GetCompanyUserSelectListAsync(viewModel.CompanyId);
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
                PersonsCollectingSamples = viewModel.PersonsCollectingSamples,
                MineralizationRatePercent = viewModel.MineralizationRatePercent ?? 40m,
                VolatilizationRatePercent = viewModel.VolatilizationRatePercent ?? 50m,
                LagoonBermHeightFeet = viewModel.LagoonBermHeightFeet
            };

            await _facilityService.CreateAsync(facility);

            if (ShouldAssignOrc(viewModel.OrcUserId, viewModel.OrcName, viewModel.OrcAssignmentStartDate))
            {
                var displayName = await ResolveOrcDisplayNameAsync(viewModel.OrcUserId, viewModel.OrcName);
                await _facilityOrcAssignmentService.AssignAsync(
                    facility.Id,
                    viewModel.OrcUserId,
                    viewModel.OrcAssignmentStartDate!.Value,
                    displayName,
                    viewModel.OperatorNumber,
                    viewModel.OperatorGrade,
                    viewModel.OperatorPhone);
            }

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
                ViewBag.Companies = await GetCompanySelectListAsync();
                ViewBag.CompanyUsers = await GetCompanyUserSelectListAsync(viewModel.CompanyId);
                return PartialView("Partials/_FacilityCreateFormPartial", viewModel);
            }

            ViewBag.Companies = await GetCompanySelectListAsync();
            ViewBag.CompanyUsers = await GetCompanyUserSelectListAsync(viewModel.CompanyId);
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

        await EnsureCompanyAccessAsync(facility.CompanyId);

        var viewModel = await BuildFacilityEditViewModelAsync(facility);
        ViewBag.Companies = await GetCompanySelectListAsync();
        ViewBag.FacilityPermits = await GetFacilityPermitSelectListAsync(facility.Id, viewModel.DefaultFacilityPermitId);
        ViewBag.CompanyUsers = await GetCompanyUserSelectListAsync(facility.CompanyId);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> FacilityEdit(FacilityEditViewModel viewModel)
    {
        await EnsureCompanyAccessAsync(viewModel.CompanyId);

        if (!ModelState.IsValid)
        {
            await PopulateFacilityEditLookupsAsync(viewModel);
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
            facility.PersonsCollectingSamples = viewModel.PersonsCollectingSamples;
            facility.MineralizationRatePercent = viewModel.MineralizationRatePercent;
            facility.VolatilizationRatePercent = viewModel.VolatilizationRatePercent;
            facility.DefaultFacilityPermitId = viewModel.DefaultFacilityPermitId;
            facility.LagoonBermHeightFeet = viewModel.LagoonBermHeightFeet;

            await _facilityService.UpdateAsync(facility);

            if (viewModel.EndCurrentOrcAsOf.HasValue && !ShouldAssignOrc(viewModel.OrcUserId, viewModel.OrcName, viewModel.OrcAssignmentStartDate))
            {
                await _facilityOrcAssignmentService.EndCurrentAsync(facility.Id, viewModel.EndCurrentOrcAsOf.Value);
            }

            if (ShouldAssignOrc(viewModel.OrcUserId, viewModel.OrcName, viewModel.OrcAssignmentStartDate))
            {
                var displayName = await ResolveOrcDisplayNameAsync(viewModel.OrcUserId, viewModel.OrcName);
                await _facilityOrcAssignmentService.AssignAsync(
                    facility.Id,
                    viewModel.OrcUserId,
                    viewModel.OrcAssignmentStartDate!.Value,
                    displayName,
                    viewModel.OperatorNumber,
                    viewModel.OperatorGrade,
                    viewModel.OperatorPhone);
            }
            else
            {
                await _facilityOrcAssignmentService.UpdateCurrentSnapshotAsync(
                    facility.Id,
                    viewModel.OperatorNumber,
                    viewModel.OperatorGrade,
                    viewModel.OperatorPhone,
                    viewModel.OrcName);
            }

            TempData["SuccessMessage"] = $"Facility '{facility.Name}' updated successfully.";
            return RedirectToAction("SystemAdmin", new { tab = "facilities" });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await PopulateFacilityEditLookupsAsync(viewModel);
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

        return RedirectToAction("SystemAdmin", new { tab = "facilities" });
    }

    private async Task<FacilityEditViewModel> BuildFacilityEditViewModelAsync(Facility facility)
    {
        var currentOrc = await _facilityOrcAssignmentService.GetCurrentAsync(facility.Id);
        var history = await _facilityOrcAssignmentService.GetByFacilityIdAsync(facility.Id);

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
            CurrentOrcUserId = currentOrc?.UserId,
            CurrentOrcStartDate = currentOrc?.StartDate,
            OrcAssignmentHistory = history.Select(MapOrcAssignmentItem).ToList(),
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

        return viewModel;
    }

    private async Task PopulateFacilityEditLookupsAsync(FacilityEditViewModel viewModel)
    {
        var history = await _facilityOrcAssignmentService.GetByFacilityIdAsync(viewModel.Id);
        var current = history.FirstOrDefault(x => x.EndDate == null);
        viewModel.OrcAssignmentHistory = history.Select(MapOrcAssignmentItem).ToList();
        viewModel.CurrentOrcUserId = current?.UserId;
        viewModel.CurrentOrcStartDate = current?.StartDate;

        ViewBag.Companies = await GetCompanySelectListAsync();
        ViewBag.FacilityPermits = await GetFacilityPermitSelectListAsync(viewModel.Id, viewModel.DefaultFacilityPermitId);
        ViewBag.CompanyUsers = await GetCompanyUserSelectListAsync(viewModel.CompanyId);
    }

    private async Task<SelectList> GetCompanyUserSelectListAsync(Guid companyId)
    {
        if (companyId == Guid.Empty)
        {
            return new SelectList(Enumerable.Empty<SelectListItem>());
        }

        var users = await _context.Users.AsNoTracking()
            .Where(u => u.CompanyId == companyId && u.IsActive)
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Email)
            .Select(u => new
            {
                u.Id,
                Label = string.IsNullOrWhiteSpace(u.FullName)
                    ? u.Email
                    : $"{u.FullName} ({u.Email})"
            })
            .ToListAsync();

        return new SelectList(users, "Id", "Label");
    }

    private async Task<string> ResolveOrcDisplayNameAsync(string? userId, string? fallbackName)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                if (!string.IsNullOrWhiteSpace(user.FullName))
                {
                    return user.FullName.Trim();
                }

                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    return user.Email.Trim();
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(fallbackName))
        {
            return fallbackName.Trim();
        }

        throw new Infrastructure.Exceptions.BusinessRuleException("ORC name or SAM user is required to create an assignment.");
    }

    private static bool ShouldAssignOrc(string? userId, string? orcName, DateTime? startDate)
        => startDate.HasValue && (!string.IsNullOrWhiteSpace(userId) || !string.IsNullOrWhiteSpace(orcName));

    private static FacilityOrcAssignmentItemViewModel MapOrcAssignmentItem(FacilityOrcAssignment row)
        => new()
        {
            Id = row.Id,
            DisplayName = row.DisplayName,
            UserName = row.User?.FullName
                ?? (string.IsNullOrWhiteSpace(row.User?.Email) ? null : row.User.Email),
            StartDate = row.StartDate,
            EndDate = row.EndDate,
            OperatorNumber = row.OperatorNumber,
            OperatorGrade = row.OperatorGrade,
            OperatorPhone = row.OperatorPhone
        };

    #endregion
}
