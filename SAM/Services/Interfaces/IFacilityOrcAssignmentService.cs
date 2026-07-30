using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

public interface IFacilityOrcAssignmentService
{
    Task<IReadOnlyList<FacilityOrcAssignment>> GetByFacilityIdAsync(Guid facilityId);

    Task<FacilityOrcAssignment?> GetCurrentAsync(Guid facilityId);

    Task<FacilityOrcAssignment> AssignAsync(
        Guid facilityId,
        string? userId,
        DateTime startDate,
        string displayName,
        string? operatorNumber,
        string? operatorGrade,
        string? operatorPhone);

    Task EndCurrentAsync(Guid facilityId, DateTime endDate);

    Task UpdateCurrentSnapshotAsync(
        Guid facilityId,
        string? operatorNumber,
        string? operatorGrade,
        string? operatorPhone,
        string? displayName = null);

    Task SyncFacilityMirrorFromCurrentAsync(Guid facilityId);

    Task<bool> HasOrcChangedSincePreviousAsync(Guid facilityId, int reportYear, int reportMonth);
}
