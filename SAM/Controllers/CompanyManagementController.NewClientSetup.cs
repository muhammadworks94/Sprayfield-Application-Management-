using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SAM.Infrastructure.Exceptions;
using SAM.ViewModels.CompanyManagement;

namespace SAM.Controllers;

public partial class CompanyManagementController
{
    [HttpGet]
    public IActionResult NewClientSetup()
    {
        var defaultThrough = DateTime.UtcNow.AddMonths(-1);
        var viewModel = new NewClientSetupPage1ViewModel
        {
            FirstReportingMonth = defaultThrough.Month,
            FirstReportingYear = defaultThrough.Year,
            FacilityCount = 1,
            Facilities = BuildDefaultFacilities(1)
        };

        ViewBag.Months = GetMonthSelectList(viewModel.FirstReportingMonth);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewClientSetup(NewClientSetupPage1ViewModel viewModel)
    {
        ViewBag.Months = GetMonthSelectList(viewModel.FirstReportingMonth);
        NormalizePage1ViewModel(viewModel);

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        try
        {
            var companyId = await _clientSetupWizardService.ProvisionCompanyAsync(
                viewModel,
                CurrentUserEmail ?? "system");

            return RedirectToAction(nameof(NewClientSetupBaseline), new { companyId });
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(viewModel);
        }
    }

    [HttpGet]
    public async Task<IActionResult> NewClientSetupBaseline(Guid companyId)
    {
        var company = await _companyService.GetByIdAsync(companyId);
        if (company == null)
        {
            return NotFound();
        }

        var throughYear = company.FirstReportingYear ?? DateTime.UtcNow.Year;
        var throughMonth = company.FirstReportingMonth ?? DateTime.UtcNow.Month;

        var viewModel = await _baselineMonthlyLoadingService.GetWizardGridAsync(companyId, throughYear, throughMonth);
        ViewBag.Months = GetMonthSelectList(throughMonth);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FinishNewClientSetupBaseline(NewClientSetupBaselineViewModel viewModel)
    {
        var company = await _companyService.GetByIdAsync(viewModel.CompanyId);
        if (company == null)
        {
            return NotFound();
        }

        var savedCellCount = await _baselineMonthlyLoadingService.CountSavedBaselineCellsAsync(viewModel.CompanyId);
        if (savedCellCount == 0)
        {
            TempData["WarningMessage"] = "No baseline loading values were saved. You can return later to enter data.";
        }

        if (viewModel.RefreshNdarAfterFinish)
        {
            await _baselineMonthlyLoadingService.RefreshNdarReportsForCompanyWindowAsync(
                viewModel.CompanyId,
                viewModel.ThroughYear,
                viewModel.ThroughMonth);
        }

        TempData["SuccessMessage"] = savedCellCount > 0
            ? "Company created. Set Verified to ON on Manage Companies to activate."
            : "Company created without baseline data. Set Verified to ON on Manage Companies to activate.";

        return RedirectToAction(nameof(Index), new { tab = "companies" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBaselineCell([FromBody] ClientSetupCellSaveRequest request)
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
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCompanyVerified([FromBody] ToggleCompanyVerifiedRequest request)
    {
        if (request == null || request.CompanyId == Guid.Empty)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == request.CompanyId);
        if (company == null)
        {
            return NotFound(new { success = false, message = "Company not found." });
        }

        company.IsVerified = request.IsVerified;
        company.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            isVerified = company.IsVerified,
            message = company.IsVerified ? "Company verified." : "Company marked as pending verification."
        });
    }

    [HttpGet]
    public IActionResult ClientSetup()
    {
        return RedirectToAction(nameof(NewClientSetup));
    }

    private static List<NewClientSetupFacilityViewModel> BuildDefaultFacilities(int count)
    {
        var facilities = new List<NewClientSetupFacilityViewModel>();
        for (var i = 0; i < count; i++)
        {
            facilities.Add(new NewClientSetupFacilityViewModel
            {
                Name = $"Facility {i + 1}",
                SprayfieldCount = 1,
                SprayfieldFieldCodes = new List<string> { "1" }
            });
        }

        return facilities;
    }

    private static void NormalizePage1ViewModel(NewClientSetupPage1ViewModel viewModel)
    {
        viewModel.FacilityCount = Math.Clamp(viewModel.FacilityCount, 1, 10);
        viewModel.Facilities ??= new List<NewClientSetupFacilityViewModel>();

        while (viewModel.Facilities.Count < viewModel.FacilityCount)
        {
            var index = viewModel.Facilities.Count;
            viewModel.Facilities.Add(new NewClientSetupFacilityViewModel
            {
                Name = $"Facility {index + 1}",
                SprayfieldCount = 1,
                SprayfieldFieldCodes = new List<string> { "1" }
            });
        }

        if (viewModel.Facilities.Count > viewModel.FacilityCount)
        {
            viewModel.Facilities = viewModel.Facilities.Take(viewModel.FacilityCount).ToList();
        }

        foreach (var facility in viewModel.Facilities)
        {
            facility.SprayfieldCount = Math.Clamp(facility.SprayfieldCount, 1, 150);
            facility.SprayfieldFieldCodes ??= new List<string>();

            while (facility.SprayfieldFieldCodes.Count < facility.SprayfieldCount)
            {
                facility.SprayfieldFieldCodes.Add((facility.SprayfieldFieldCodes.Count + 1).ToString());
            }

            if (facility.SprayfieldFieldCodes.Count > facility.SprayfieldCount)
            {
                facility.SprayfieldFieldCodes = facility.SprayfieldFieldCodes.Take(facility.SprayfieldCount).ToList();
            }
        }
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
