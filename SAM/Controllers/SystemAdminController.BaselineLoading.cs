using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAM.Infrastructure.Authorization;
using SAM.Infrastructure.Exceptions;
using SAM.ViewModels.CompanyManagement;

namespace SAM.Controllers;

public partial class SystemAdminController
{
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    [HttpGet]
    public async Task<IActionResult> ModifyBaselineLoading(Guid facilityId)
    {
        var facility = await _facilityService.GetByIdAsync(facilityId);
        if (facility == null)
        {
            return NotFound();
        }

        await EnsureCompanyAccessAsync(facility.CompanyId);

        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == facility.CompanyId);
        if (company == null)
        {
            return NotFound();
        }

        var throughYear = company.FirstReportingYear ?? DateTime.UtcNow.Year;
        var throughMonth = company.FirstReportingMonth ?? DateTime.UtcNow.Month;

        var viewModel = await _baselineMonthlyLoadingService.GetWizardGridAsync(
            facility.CompanyId,
            throughYear,
            throughMonth);

        viewModel.FacilityGroups = viewModel.FacilityGroups
            .Where(g => g.FacilityId == facilityId)
            .ToList();
        viewModel.FacilityId = facilityId;
        viewModel.FacilityName = facility.Name;
        viewModel.IsFacilityEditMode = true;

        return View(viewModel);
    }

    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FinishModifyBaselineLoading(NewClientSetupBaselineViewModel viewModel)
    {
        if (viewModel.FacilityId == null || viewModel.FacilityId == Guid.Empty)
        {
            return RedirectToAction(nameof(SystemAdmin), new { tab = "facilities" });
        }

        var facility = await _facilityService.GetByIdAsync(viewModel.FacilityId.Value);
        if (facility == null)
        {
            return NotFound();
        }

        await EnsureCompanyAccessAsync(facility.CompanyId);

        TempData["SuccessMessage"] = "Baseline loading changes have been saved.";
        return RedirectToAction(nameof(FacilityEdit), new { id = viewModel.FacilityId.Value });
    }

    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBaselineLoadingCell([FromBody] ClientSetupCellSaveRequest request)
    {
        if (request == null || request.FacilityId == Guid.Empty || request.SprayfieldId == Guid.Empty)
        {
            return BadRequest(new ClientSetupCellSaveResponse { Success = false, Message = "Invalid cell payload." });
        }

        var facility = await _facilityService.GetByIdAsync(request.FacilityId);
        if (facility == null)
        {
            return NotFound(new ClientSetupCellSaveResponse { Success = false, Message = "Facility not found." });
        }

        try
        {
            await EnsureCompanyAccessAsync(facility.CompanyId);

            await _baselineMonthlyLoadingService.SaveSetupCellAsync(
                request.FacilityId,
                request.SprayfieldId,
                request.ThroughYear,
                request.ThroughMonth,
                request.Year,
                request.Month,
                request.LoadingInches,
                CurrentUserEmail ?? "system");

            return Json(new ClientSetupCellSaveResponse { Success = true });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new ClientSetupCellSaveResponse { Success = false, Message = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new ClientSetupCellSaveResponse { Success = false, Message = ex.Message });
        }
        catch (UnauthorizedResourceAccessException)
        {
            return Forbid();
        }
    }
}
