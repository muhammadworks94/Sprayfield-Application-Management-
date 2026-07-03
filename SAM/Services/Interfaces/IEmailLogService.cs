using SAM.Domain.Entities;
using SAM.Services.Models;

namespace SAM.Services.Interfaces;

public interface IEmailLogService
{
    Task<EmailLog> CreateAsync(EmailLogCreateModel model, CancellationToken cancellationToken = default);
    Task<PagedResult<EmailLog>> QueryAsync(EmailLogQueryModel query, CancellationToken cancellationToken = default);
    Task<EmailLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetTemplateKeysAsync(CancellationToken cancellationToken = default);
}
