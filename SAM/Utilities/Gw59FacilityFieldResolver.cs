using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;

namespace SAM.Utilities;

public static class Gw59FacilityFieldResolver
{
    public static string ResolveContactPerson(Facility? facility) =>
        facility?.FacilityContactPerson ?? string.Empty;

    public static string ResolveFacilityPhone(Facility? facility) =>
        facility?.FacilityContactPersonPhone ?? string.Empty;

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
}
