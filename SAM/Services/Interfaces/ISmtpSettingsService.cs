using SAM.Domain.Entities;
using SAM.Services.Models;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service contract for managing and resolving SMTP settings.
/// </summary>
public interface ISmtpSettingsService
{
    Task<SmtpSettings?> GetGlobalAsync();
    Task UpsertGlobalAsync(SmtpSettingsUpdateModel input);
    Task<SmtpRuntimeOptions> GetRuntimeOptionsAsync();
}
