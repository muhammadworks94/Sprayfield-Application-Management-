using SAM.Domain.Entities;
using SAM.Services.Models;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service contract for managing and rendering system email templates.
/// </summary>
public interface IEmailTemplateService
{
    Task<IReadOnlyList<EmailTemplate>> GetSystemTemplatesAsync();
    Task<EmailTemplate?> GetByKeyAsync(string templateKey);
    Task UpdateSystemTemplateAsync(string templateKey, string subjectTemplate, string bodyTemplate);
    Task<RenderedEmail> RenderAsync(string templateKey, IReadOnlyDictionary<string, string> tokens);
}
