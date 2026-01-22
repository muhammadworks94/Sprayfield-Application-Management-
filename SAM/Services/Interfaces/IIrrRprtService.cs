using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for IrrRprt (Monthly Irrigation Report) entity operations.
/// </summary>
public interface IIrrRprtService
{
    Task<IEnumerable<IrrRprt>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null);
    Task<IrrRprt?> GetByIdAsync(Guid id);
    Task<IrrRprt> CreateAsync(IrrRprt irrRprt);
    Task<IrrRprt> UpdateAsync(IrrRprt irrRprt);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<IrrRprt?> GetByFacilityMonthYearAsync(Guid facilityId, int month, int year);
    Task<IEnumerable<IrrRprt>> GetByFacilityIdAsync(Guid facilityId);
    Task<IrrRprt> GenerateMonthlyReportAsync(Guid facilityId, int month, int year);
}
