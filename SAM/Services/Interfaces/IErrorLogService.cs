using SAM.Domain.Entities;
using SAM.Services.Models;

namespace SAM.Services.Interfaces;

public interface IErrorLogService
{
    Task<ErrorLog> CreateAsync(ErrorLogCreateModel model, CancellationToken cancellationToken = default);
    Task<PagedResult<ErrorLog>> QueryAsync(ErrorLogQueryModel query, CancellationToken cancellationToken = default);
    Task<int> PurgeOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApplicationUser>> GetActorsAsync(Guid? companyId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetModulesAsync(Guid? companyId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetExceptionTypesAsync(Guid? companyId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> GetStatusCodesAsync(Guid? companyId = null, CancellationToken cancellationToken = default);
}

