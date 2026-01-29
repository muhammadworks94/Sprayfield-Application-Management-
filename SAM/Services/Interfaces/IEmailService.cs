using System.Threading.Tasks;

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
    Task SendEmailAsync(string to, string subject, string htmlBody);
}


