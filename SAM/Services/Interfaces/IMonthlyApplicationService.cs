using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

public interface IMonthlyApplicationService
{
    Task<IEnumerable<MonthlyApplication>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null, Guid? sprayfieldId = null);
    Task<MonthlyApplication?> GetByIdAsync(Guid id);
    Task<MonthlyApplication> CreateAsync(MonthlyApplication application);
    Task<MonthlyApplication> UpdateAsync(MonthlyApplication application);
    Task<bool> DeleteAsync(Guid id);
}
