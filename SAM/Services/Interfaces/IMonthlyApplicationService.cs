using SAM.Domain.Entities;
using SAM.Services.Models;

namespace SAM.Services.Interfaces;

public interface IMonthlyApplicationService
{
    Task<IEnumerable<MonthlyApplication>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null, Guid? sprayfieldId = null);
    Task<IEnumerable<MonthlyApplication>> GetByDateRangeAsync(Guid? companyId, DateTime startDate, DateTime endDate);
    Task<MonthlyApplication?> GetByIdAsync(Guid id);
    Task<Guid> GetCompanyIdAsync(Guid id);
    Task<MonthlyApplication> CreateAsync(MonthlyApplication application);
    Task<MonthlyApplication> UpdateAsync(MonthlyApplication application);
    Task<bool> DeleteAsync(Guid id);
    Task<MonthlyApplicationMutationResult> CreateWithNdarRefreshAsync(MonthlyApplication application);
    Task<MonthlyApplicationMutationResult> UpdateWithNdarRefreshAsync(MonthlyApplication application);
    Task<(bool Deleted, Guid CompanyId, Guid FacilityId, List<NdarRefreshOutcome> NdarRefreshOutcomes)> DeleteWithNdarRefreshAsync(Guid id);
}
