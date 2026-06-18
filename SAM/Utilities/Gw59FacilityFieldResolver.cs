using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.ViewModels.SystemAdmin;

namespace SAM.Utilities;

public static class Gw59FacilityFieldResolver
{
    public static string ResolveContactPerson(Facility? facility) =>
        facility?.FacilityContactPerson ?? string.Empty;

    public static string ResolveFacilityPhone(Facility? facility) =>
        facility?.FacilityContactPersonPhone ?? string.Empty;

    public static string ResolveAddress(Facility? facility, FacilityPermit? permit) =>
        permit?.Address?.Trim() ?? string.Empty;

    public static string ResolveCity(Facility? facility, FacilityPermit? permit) =>
        permit?.City?.Trim() ?? string.Empty;

    public static string ResolveState(Facility? facility, FacilityPermit? permit) =>
        permit?.State?.Trim() ?? string.Empty;

    public static string ResolveZipCode(Facility? facility, FacilityPermit? permit) =>
        permit?.ZipCode?.Trim() ?? string.Empty;

    public static string ResolveCounty(Facility? facility, FacilityPermit? permit) =>
        permit?.County?.Trim() ?? string.Empty;

    public static string ResolvePermitNumber(Facility? facility, FacilityPermit? permit) =>
        permit?.PermitNumber?.Trim() ?? string.Empty;

    public static string ResolvePermitNumberForReport(Facility? facility, FacilityPermit? permit)
    {
        if (permit != null && !string.IsNullOrWhiteSpace(permit.PermitNumber))
        {
            return $"{permit.PermitNumber} v{permit.PermitVersion}";
        }

        return string.Empty;
    }

    public static int? ResolveNumberOfWellsToBeSampled(Facility? facility, FacilityPermit? permit, int monitoringWellCount) =>
        permit?.TotalNumberOfSprayfields ?? monitoringWellCount;

    public static (string LabName, string LabCertificationNumber) ResolveLabInfo(
        Facility? facility,
        CompanyLabOption? labOption)
    {
        if (labOption != null)
        {
            return (labOption.Name, labOption.CertificationNumber ?? string.Empty);
        }

        return (string.Empty, string.Empty);
    }

    public static FacilityPermit? ResolveDisplayPermit(Facility facility, IReadOnlyList<FacilityPermit> activePermitsForFacility)
    {
        if (facility.DefaultFacilityPermitId is Guid permitId)
        {
            var selected = activePermitsForFacility.FirstOrDefault(p => p.Id == permitId);
            if (selected != null)
            {
                return selected;
            }
        }

        return activePermitsForFacility
            .OrderByDescending(p => p.EffectiveStartDate)
            .FirstOrDefault();
    }

    public static async Task<Dictionary<Guid, List<FacilityPermit>>> LoadActivePermitsByFacilityAsync(
        ApplicationDbContext context,
        IEnumerable<Guid> facilityIds)
    {
        var ids = facilityIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, List<FacilityPermit>>();
        }

        var permits = await context.FacilityPermits
            .AsNoTracking()
            .Where(p => ids.Contains(p.FacilityId) && p.IsActive)
            .OrderByDescending(p => p.EffectiveStartDate)
            .ToListAsync();

        return permits
            .GroupBy(p => p.FacilityId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public static FacilityListItemViewModel ToFacilityListItemViewModel(Facility facility) =>
        new()
        {
            Id = facility.Id,
            CompanyId = facility.CompanyId,
            CompanyName = facility.Company?.Name,
            Name = facility.Name,
            Permittee = facility.Permittee,
            FacilityClass = facility.FacilityClass
        };

    public static FacilityViewModel ToFacilityListViewModel(Facility facility, FacilityPermit? displayPermit) =>
        new()
        {
            Id = facility.Id,
            CompanyId = facility.CompanyId,
            CompanyName = facility.Company?.Name,
            Name = facility.Name,
            Permittee = facility.Permittee,
            PermitNumber = ResolvePermitNumber(facility, displayPermit),
            City = ResolveCity(facility, displayPermit),
            State = ResolveState(facility, displayPermit),
            County = ResolveCounty(facility, displayPermit),
            Address = ResolveAddress(facility, displayPermit),
            ZipCode = ResolveZipCode(facility, displayPermit)
        };

    public static Task<int> CountMonitoringWellsForFacilityAsync(
        ApplicationDbContext context,
        Guid facilityId,
        Guid companyId)
    {
        return context.MonitoringWells
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .CountAsync();
    }

    public static async Task<string> ResolveWellLocationAsync(
        ApplicationDbContext context,
        Guid monitoringWellId,
        MonitoringWell? monitoringWell = null)
    {
        if (!string.IsNullOrWhiteSpace(monitoringWell?.LocationDescription))
        {
            return monitoringWell.LocationDescription;
        }

        return await context.MonitoringWells
            .AsNoTracking()
            .Where(w => w.Id == monitoringWellId)
            .Select(w => w.LocationDescription)
            .FirstOrDefaultAsync() ?? string.Empty;
    }

    public static async Task<CompanyLabOption?> ResolveLabOptionAsync(
        ApplicationDbContext context,
        Facility? facility)
    {
        if (facility?.DefaultLabOptionId is Guid labOptionId)
        {
            return await context.CompanyLabOptions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == labOptionId && x.IsActive);
        }

        if (facility == null)
        {
            return null;
        }

        var companyLabs = await context.CompanyLabOptions
            .AsNoTracking()
            .Where(x => x.CompanyId == facility.CompanyId && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        return companyLabs.Count == 1 ? companyLabs[0] : null;
    }

    public static async Task<FacilityPermit?> ResolvePreferredPermitAsync(
        ApplicationDbContext context,
        Facility? facility,
        FacilityPermit? dateResolvedPermit)
    {
        if (facility?.DefaultFacilityPermitId is Guid permitId)
        {
            var selected = await context.FacilityPermits
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == permitId && p.FacilityId == facility.Id);
            if (selected != null)
            {
                return selected;
            }
        }

        return dateResolvedPermit;
    }
}
