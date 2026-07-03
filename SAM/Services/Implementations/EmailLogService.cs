using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Services.Interfaces;
using SAM.Services.Models;

namespace SAM.Services.Implementations;

public class EmailLogService : IEmailLogService
{
    private readonly ApplicationDbContext _context;

    public EmailLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EmailLog> CreateAsync(EmailLogCreateModel model, CancellationToken cancellationToken = default)
    {
        var emailLog = new EmailLog
        {
            SentAtUtc = model.SentAtUtc,
            ToEmail = Truncate(model.ToEmail, 256),
            Subject = Truncate(model.Subject, 500),
            TemplateKey = Truncate(model.TemplateKey, 100),
            TemplateDisplayName = Truncate(model.TemplateDisplayName, 150),
            Status = model.Status,
            ErrorMessage = Truncate(model.ErrorMessage, 2000),
            HtmlBody = Truncate(model.HtmlBody, 8000),
            InitiatedByUserId = Truncate(model.InitiatedByUserId, 450),
            InitiatedByEmail = Truncate(model.InitiatedByEmail, 256),
            InitiatedByDisplayName = Truncate(model.InitiatedByDisplayName, 200)
        };

        _context.EmailLogs.Add(emailLog);
        await _context.SaveChangesAsync(cancellationToken);

        return emailLog;
    }

    public async Task<EmailLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EmailLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PagedResult<EmailLog>> QueryAsync(EmailLogQueryModel query, CancellationToken cancellationToken = default)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : Math.Min(query.PageSize, 200);

        var q = _context.EmailLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.TemplateKey))
        {
            q = q.Where(x => x.TemplateKey == query.TemplateKey);
        }

        if (query.Status.HasValue)
        {
            q = q.Where(x => x.Status == query.Status.Value);
        }

        if (query.FromUtc.HasValue)
        {
            q = q.Where(x => x.SentAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            var toExclusive = query.ToUtc.Value.Date.AddDays(1);
            q = q.Where(x => x.SentAtUtc < toExclusive);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            q = q.Where(x =>
                x.ToEmail.Contains(term) ||
                x.Subject.Contains(term) ||
                (x.TemplateDisplayName != null && x.TemplateDisplayName.Contains(term)) ||
                (x.InitiatedByEmail != null && x.InitiatedByEmail.Contains(term)) ||
                (x.InitiatedByDisplayName != null && x.InitiatedByDisplayName.Contains(term)));
        }

        var totalCount = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(x => x.SentAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<EmailLog>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<string>> GetTemplateKeysAsync(CancellationToken cancellationToken = default)
    {
        return await _context.EmailLogs
            .AsNoTracking()
            .Where(x => x.TemplateKey != null && x.TemplateKey != string.Empty)
            .Select(x => x.TemplateKey!)
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
