using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Services.Interfaces;
using SAM.Services.Models;

namespace SAM.Services.Implementations;

public class UserActivityLogService : IUserActivityLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserActivityLogService> _logger;

    public UserActivityLogService(ApplicationDbContext context, ILogger<UserActivityLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<UserActivityLog>> QueryAsync(UserActivityLogQueryModel query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : Math.Min(query.PageSize, 200);

        var q = _context.UserActivityLogs.AsNoTracking().AsQueryable();

        if (query.CompanyId.HasValue)
        {
            q = q.Where(x => x.CompanyId == query.CompanyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.ActorUserId))
        {
            q = q.Where(x => x.ActorUserId == query.ActorUserId);
        }

        if (query.ActivityType.HasValue)
        {
            q = q.Where(x => x.ActivityType == query.ActivityType.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Module))
        {
            q = q.Where(x => x.Module == query.Module);
        }

        if (query.FromUtc.HasValue)
        {
            q = q.Where(x => x.OccurredAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            q = q.Where(x => x.OccurredAtUtc <= query.ToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim().ToLower();
            q = q.Where(x =>
                x.ActorEmail.ToLower().Contains(search) ||
                x.EntityName.ToLower().Contains(search) ||
                x.Summary.ToLower().Contains(search) ||
                (x.Path != null && x.Path.ToLower().Contains(search)));
        }

        var totalCount = await q.CountAsync();
        var items = await q
            .OrderByDescending(x => x.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<UserActivityLog>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task LogAuthenticationEventAsync(UserActivityType activityType, ApplicationUser? user, HttpContext? httpContext = null)
    {
        if (activityType is not UserActivityType.Login and not UserActivityType.Logout)
        {
            return;
        }

        var actorEmail = user?.Email ?? httpContext?.User?.Identity?.Name ?? "system";
        var actorUserId = user?.Id ?? httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var actorDisplayName = user?.FullName ?? httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        var log = new UserActivityLog
        {
            OccurredAtUtc = DateTime.UtcNow,
            ActivityType = activityType,
            Module = "Account",
            EntityName = nameof(ApplicationUser),
            EntityId = user?.Id,
            CompanyId = user?.CompanyId,
            ActorUserId = actorUserId,
            ActorEmail = actorEmail,
            ActorDisplayName = actorDisplayName,
            HttpMethod = httpContext?.Request?.Method,
            Path = httpContext?.Request?.Path.Value,
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
            Summary = $"{actorEmail} {activityType}",
            CorrelationId = httpContext?.TraceIdentifier
        };

        _context.UserActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<int> PurgeOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        var oldRows = await _context.UserActivityLogs
            .Where(x => x.OccurredAtUtc < cutoffUtc)
            .ToListAsync(cancellationToken);

        if (oldRows.Count == 0)
        {
            return 0;
        }

        _context.UserActivityLogs.RemoveRange(oldRows);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Purged {Count} user activity logs older than {CutoffUtc}", oldRows.Count, cutoffUtc);
        return oldRows.Count;
    }

    public async Task<IReadOnlyList<ApplicationUser>> GetActorsAsync(Guid? companyId = null)
    {
        var userIds = await _context.UserActivityLogs
            .AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.ActorUserId))
            .Where(x => !companyId.HasValue || x.CompanyId == companyId.Value)
            .Select(x => x.ActorUserId!)
            .Distinct()
            .ToListAsync();

        if (userIds.Count == 0)
        {
            return Array.Empty<ApplicationUser>();
        }

        return await _context.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .OrderBy(x => x.FullName)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetModulesAsync(Guid? companyId = null)
    {
        return await _context.UserActivityLogs
            .AsNoTracking()
            .Where(x => !companyId.HasValue || x.CompanyId == companyId.Value)
            .Select(x => x.Module)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }
}
