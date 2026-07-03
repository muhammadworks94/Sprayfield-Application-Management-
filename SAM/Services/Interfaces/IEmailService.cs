using System.Threading.Tasks;
using SAM.Services.Models;

namespace SAM.Services.Interfaces;

/// <summary>
/// Abstraction for sending emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email asynchronously.
    /// </summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="subject">Email subject.</param>
    /// <param name="htmlBody">HTML body content.</param>
    /// <param name="context">Optional context for email logging.</param>
    Task SendEmailAsync(string to, string subject, string htmlBody, EmailSendContext? context = null);
}
