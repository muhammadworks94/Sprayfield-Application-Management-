using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

public interface IFacilityPermitResolver
{
    Task<FacilityPermit?> ResolveForDateAsync(Guid facilityId, DateTime reportDate);
}

