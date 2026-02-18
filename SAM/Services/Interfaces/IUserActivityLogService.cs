using Microsoft.AspNetCore.Http;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Services.Models;

namespace SAM.Services.Interfaces;

public interface IUserActivityLogService
{
    Task<PagedResult<UserActivityLog>> QueryAsync(UserActivityLogQueryModel query);
    Task LogAuthenticationEventAsync(UserActivityType activityType, ApplicationUser? user, HttpContext? httpContext = null);
    Task<int> PurgeOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApplicationUser>> GetActorsAsync(Guid? companyId = null);
    Task<IReadOnlyList<string>> GetModulesAsync(Guid? companyId = null);
}
