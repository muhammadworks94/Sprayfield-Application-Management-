using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Services.Interfaces;
using SAM.Services.Models;

namespace SAM.Services.Implementations;

public class ErrorLogService : IErrorLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ErrorLogService> _logger;

    public ErrorLogService(ApplicationDbContext context, ILogger<ErrorLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ErrorLog> CreateAsync(ErrorLogCreateModel model, CancellationToken cancellationToken = default)
    {
        var companyId = model.CompanyId;
        if (!companyId.HasValue && !string.IsNullOrWhiteSpace(model.ActorUserId))
        {
            companyId = await _context.Users
                .Where(x => x.Id == model.ActorUserId)
                .Select(x => x.CompanyId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var errorLog = new ErrorLog
        {
            OccurredAtUtc = model.OccurredAtUtc,
            ExceptionType = Truncate(model.ExceptionType, 300),
            Message = Truncate(model.Message, 2000),
            StatusCode = model.StatusCode,
            Module = Truncate(string.IsNullOrWhiteSpace(model.Module) ? "Unknown" : model.Module, 100),
            Path = Truncate(model.Path, 1024),
            HttpMethod = Truncate(model.HttpMethod, 16),
            RequestId = Truncate(model.RequestId, 128),
            CorrelationId = Truncate(model.CorrelationId, 128),
            ActorUserId = Truncate(model.ActorUserId, 450),
            ActorEmail = Truncate(model.ActorEmail, 256),
            ActorDisplayName = Truncate(model.ActorDisplayName, 200),
            CompanyId = companyId,
            IpAddress = Truncate(model.IpAddress, 64),
            QueryString = Truncate(model.QueryString, 1024),
            InnerExceptionType = Truncate(model.InnerExceptionType, 300),
            InnerExceptionMessage = Truncate(model.InnerExceptionMessage, 2000)
        };

        _context.ErrorLogs.Add(errorLog);
        await _context.SaveChangesAsync(cancellationToken);

        return errorLog;
    }

    public async Task<PagedResult<ErrorLog>> QueryAsync(ErrorLogQueryModel query, CancellationToken cancellationToken = default)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : Math.Min(query.PageSize, 200);

        var q = _context.ErrorLogs.AsNoTracking().AsQueryable();

        if (query.CompanyId.HasValue)
        {
            q = q.Where(x => x.CompanyId == query.CompanyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.ActorUserId))
        {
            q = q.Where(x => x.ActorUserId == query.ActorUserId);
        }

        if (!string.IsNullOrWhiteSpace(query.Module))
        {
            q = q.Where(x => x.Module == query.Module);
        }

        if (!string.IsNullOrWhiteSpace(query.ExceptionType))
        {
            q = q.Where(x => x.ExceptionType == query.ExceptionType);
        }

        if (query.StatusCode.HasValue)
        {
            q = q.Where(x => x.StatusCode == query.StatusCode.Value);
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
                x.ExceptionType.ToLower().Contains(search) ||
                x.Message.ToLower().Contains(search) ||
                (x.Path != null && x.Path.ToLower().Contains(search)) ||
                (x.ActorEmail != null && x.ActorEmail.ToLower().Contains(search)) ||
                (x.RequestId != null && x.RequestId.ToLower().Contains(search)));
        }

        var totalCount = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(x => x.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ErrorLog>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<int> PurgeOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        var oldRows = await _context.ErrorLogs
            .Where(x => x.OccurredAtUtc < cutoffUtc)
            .ToListAsync(cancellationToken);

        if (oldRows.Count == 0)
        {
            return 0;
        }

        _context.ErrorLogs.RemoveRange(oldRows);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Purged {Count} error logs older than {CutoffUtc}", oldRows.Count, cutoffUtc);
        return oldRows.Count;
    }

    public async Task<IReadOnlyList<ApplicationUser>> GetActorsAsync(Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var userIds = await _context.ErrorLogs
            .AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.ActorUserId))
            .Where(x => !companyId.HasValue || x.CompanyId == companyId.Value)
            .Select(x => x.ActorUserId!)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (userIds.Count == 0)
        {
            return Array.Empty<ApplicationUser>();
        }

        return await _context.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .OrderBy(x => x.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetModulesAsync(Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        return await _context.ErrorLogs
            .AsNoTracking()
            .Where(x => !companyId.HasValue || x.CompanyId == companyId.Value)
            .Select(x => x.Module)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetExceptionTypesAsync(Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        return await _context.ErrorLogs
            .AsNoTracking()
            .Where(x => !companyId.HasValue || x.CompanyId == companyId.Value)
            .Select(x => x.ExceptionType)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetStatusCodesAsync(Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        return await _context.ErrorLogs
            .AsNoTracking()
            .Where(x => !companyId.HasValue || x.CompanyId == companyId.Value)
            .Select(x => x.StatusCode)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}

