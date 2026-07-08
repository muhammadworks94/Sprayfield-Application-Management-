using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Helpers;
using SAM.Services.Interfaces;
using SAM.Utilities;
using SAM.ViewModels.CompanyManagement;

namespace SAM.Services.Implementations;

public class BaselineMonthlyLoadingService : IBaselineMonthlyLoadingService
{
    private readonly ApplicationDbContext _context;
    private readonly IMonthlyLoadingResolutionService _monthlyLoadingResolution;
    private readonly INDAR1Service _ndar1Service;
    private readonly ISprayfieldService _sprayfieldService;

    public BaselineMonthlyLoadingService(
        ApplicationDbContext context,
        IMonthlyLoadingResolutionService monthlyLoadingResolution,
        INDAR1Service ndar1Service,
        ISprayfieldService sprayfieldService)
    {
        _context = context;
        _monthlyLoadingResolution = monthlyLoadingResolution;
        _ndar1Service = ndar1Service;
        _sprayfieldService = sprayfieldService;
    }

    public async Task<NewClientSetupBaselineViewModel> GetWizardGridAsync(
        Guid companyId,
        int throughYear,
        int throughMonth,
        CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);

        if (company == null)
        {
            throw new EntityNotFoundException(nameof(Company), companyId);
        }

        var facilities = await _context.Facilities
            .AsNoTracking()
            .Where(f => f.CompanyId == companyId)
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

        var facilityGroups = new List<ClientSetupFacilityGroupViewModel>();

        foreach (var facility in facilities)
        {
            var sprayfields = (await _sprayfieldService.GetByFacilityIdAsync(facility.Id))
                .OrderBy(s => int.TryParse(s.FieldId, out var order) ? order : int.MaxValue)
                .ThenBy(s => s.FieldId)
                .ToList();

            var monthRows = await BuildMonthRowsAsync(facility.Id, sprayfields, throughYear, throughMonth, cancellationToken);

            facilityGroups.Add(new ClientSetupFacilityGroupViewModel
            {
                FacilityId = facility.Id,
                FacilityName = facility.Name,
                SprayfieldColumns = sprayfields.Select(s => new ClientSetupSprayfieldColumnViewModel
                {
                    SprayfieldId = s.Id,
                    FieldCode = s.FieldId,
                    Acres = SprayfieldReportHelper.GetReportAcres(s)
                }).ToList(),
                MonthRows = monthRows
            });
        }

        var savedCellCount = await CountSavedBaselineCellsAsync(companyId, cancellationToken);

        return new NewClientSetupBaselineViewModel
        {
            CompanyId = companyId,
            CompanyName = company.Name,
            ThroughYear = throughYear,
            ThroughMonth = throughMonth,
            FacilityGroups = facilityGroups,
            SavedCellCount = savedCellCount
        };
    }

    public async Task SaveSetupCellAsync(
        Guid facilityId,
        Guid sprayfieldId,
        int throughYear,
        int throughMonth,
        int year,
        int month,
        decimal? loadingInches,
        string userId,
        CancellationToken cancellationToken = default)
    {
        await SaveCellsAsync(
            facilityId,
            throughYear,
            throughMonth,
            new[]
            {
                new ClientSetupCellSaveRequest
                {
                    SprayfieldId = sprayfieldId,
                    Year = year,
                    Month = month,
                    LoadingInches = loadingInches
                }
            },
            userId,
            cancellationToken);
    }

    public async Task<int> CountSavedBaselineCellsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _context.SprayfieldBaselineMonthlyLoadings
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.LoadingInches > 0m)
            .CountAsync(cancellationToken);
    }

    public async Task RefreshNdarReportsForCompanyWindowAsync(
        Guid companyId,
        int throughYear,
        int throughMonth,
        CancellationToken cancellationToken = default)
    {
        var facilityIds = await _context.Facilities
            .AsNoTracking()
            .Where(f => f.CompanyId == companyId)
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);

        foreach (var facilityId in facilityIds)
        {
            await RefreshNdarReportsForWindowAsync(facilityId, throughYear, throughMonth, cancellationToken);
        }
    }

    private async Task<List<ClientSetupMonthRowViewModel>> BuildMonthRowsAsync(
        Guid facilityId,
        IReadOnlyList<Sprayfield> sprayfields,
        int throughYear,
        int throughMonth,
        CancellationToken cancellationToken)
    {
        var endMonth = new DateTime(throughYear, throughMonth, 1);
        var monthRows = new List<ClientSetupMonthRowViewModel>();

        for (var i = 0; i < 12; i++)
        {
            var monthDate = endMonth.AddMonths(-i);
            var cells = new List<ClientSetupCellViewModel>();

            foreach (var sprayfield in sprayfields)
            {
                var hasReal = await _monthlyLoadingResolution.HasRealOperationalMonthAsync(
                    facilityId,
                    sprayfield.Id,
                    monthDate.Year,
                    monthDate.Month,
                    cancellationToken);

                if (hasReal)
                {
                    var inches = await _monthlyLoadingResolution.GetRealMonthlyLoadingInchesAsync(
                        facilityId,
                        sprayfield.Id,
                        monthDate.Year,
                        monthDate.Month,
                        cancellationToken);

                    cells.Add(new ClientSetupCellViewModel
                    {
                        SprayfieldId = sprayfield.Id,
                        LoadingInches = inches,
                        IsReadOnly = true,
                        Source = "Real"
                    });
                }
                else
                {
                    var baseline = await _monthlyLoadingResolution.GetBaselineMonthlyLoadingInchesAsync(
                        facilityId,
                        sprayfield.Id,
                        monthDate.Year,
                        monthDate.Month,
                        cancellationToken);

                    cells.Add(new ClientSetupCellViewModel
                    {
                        SprayfieldId = sprayfield.Id,
                        LoadingInches = baseline,
                        IsReadOnly = false,
                        Source = baseline.HasValue ? "Baseline" : "Empty"
                    });
                }
            }

            monthRows.Add(new ClientSetupMonthRowViewModel
            {
                Year = monthDate.Year,
                Month = monthDate.Month,
                Label = monthDate.ToString("MMMM yyyy"),
                Cells = cells
            });
        }

        return monthRows;
    }

    private async Task SaveCellsAsync(
        Guid facilityId,
        int throughYear,
        int throughMonth,
        IReadOnlyList<ClientSetupCellSaveRequest> cells,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var facility = await _context.Facilities
            .FirstOrDefaultAsync(f => f.Id == facilityId, cancellationToken);

        if (facility == null)
        {
            throw new EntityNotFoundException(nameof(Facility), facilityId);
        }

        var endMonth = new DateTime(throughYear, throughMonth, 1);
        var allowedMonths = Enumerable.Range(0, 12)
            .Select(offset => endMonth.AddMonths(-offset))
            .Select(d => (d.Year, d.Month))
            .ToHashSet();

        foreach (var cell in cells)
        {
            if (!allowedMonths.Contains((cell.Year, cell.Month)))
            {
                continue;
            }

            if (await _monthlyLoadingResolution.HasRealOperationalMonthAsync(
                    facilityId,
                    cell.SprayfieldId,
                    cell.Year,
                    cell.Month,
                    cancellationToken))
            {
                continue;
            }

            var existing = await _context.SprayfieldBaselineMonthlyLoadings
                .FirstOrDefaultAsync(
                    x => x.FacilityId == facilityId
                         && x.SprayfieldId == cell.SprayfieldId
                         && x.Year == cell.Year
                         && x.Month == cell.Month,
                    cancellationToken);

            if (!cell.LoadingInches.HasValue || cell.LoadingInches.Value <= 0m)
            {
                if (existing != null)
                {
                    _context.SprayfieldBaselineMonthlyLoadings.Remove(existing);
                }

                continue;
            }

            var loadingInches = Math.Round(cell.LoadingInches.Value, 2, MidpointRounding.AwayFromZero);

            if (existing == null)
            {
                _context.SprayfieldBaselineMonthlyLoadings.Add(new SprayfieldBaselineMonthlyLoading
                {
                    CompanyId = facility.CompanyId,
                    FacilityId = facilityId,
                    SprayfieldId = cell.SprayfieldId,
                    Year = cell.Year,
                    Month = cell.Month,
                    LoadingInches = loadingInches,
                    CreatedBy = userId
                });
            }
            else
            {
                existing.LoadingInches = loadingInches;
                existing.UpdatedDate = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RefreshNdarReportsForWindowAsync(
        Guid facilityId,
        int throughYear,
        int throughMonth,
        CancellationToken cancellationToken = default)
    {
        var endMonth = new DateTime(throughYear, throughMonth, 1);

        for (var i = 0; i < 12; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var monthDate = endMonth.AddMonths(-i);
            await _ndar1Service.EnsureAndRefreshForMonthAsync(facilityId, monthDate.Month, monthDate.Year);
        }
    }
}
