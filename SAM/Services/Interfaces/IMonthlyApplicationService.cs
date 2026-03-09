using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

public interface IMonthlyApplicationService
{
    Task<IEnumerable<MonthlyApplication>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null, Guid? zoneId = null);
    Task<MonthlyApplication?> GetByIdAsync(Guid id);
    Task<MonthlyApplication> CreateAsync(MonthlyApplication application);
    Task<MonthlyApplication> UpdateAsync(MonthlyApplication application);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> AnyByZoneIdAsync(Guid zoneId);
    Task<int> DeleteByZoneIdAsync(Guid zoneId);
    Task<int> ReassignZoneAsync(Guid fromZoneId, Guid toZoneId);
}
