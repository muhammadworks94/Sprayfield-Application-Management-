using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using SAM.Domain.Enums;
using SAM.Services.Interfaces;
using SAM.Services.Models;

namespace SAM.Services.Implementations;

/// <summary>
/// SMTP-based implementation of <see cref="IEmailService"/>.
/// </summary>
public class EmailService : IEmailService
{
    private readonly ISmtpSettingsService _smtpSettingsService;
    private readonly IEmailLogService _emailLogService;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        ISmtpSettingsService smtpSettingsService,
        IEmailLogService emailLogService,
        ILogger<EmailService> logger)
    {
        _smtpSettingsService = smtpSettingsService;
        _emailLogService = emailLogService;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, EmailSendContext? context = null)
    {
        var options = await _smtpSettingsService.GetRuntimeOptionsAsync();

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(options.FromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            var secureSocketOptions = options.EnableSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(options.Host, options.Port, secureSocketOptions);

            if (!string.IsNullOrWhiteSpace(options.Username))
            {
                await client.AuthenticateAsync(options.Username, options.Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {Email} with subject {Subject}.", to, subject);

            await TryLogAsync(to, subject, htmlBody, context, EmailLogStatus.Sent, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email} with subject {Subject}.", to, subject);

            await TryLogAsync(to, subject, htmlBody, context, EmailLogStatus.Failed, ex.Message);
            throw;
        }
    }

    private async Task TryLogAsync(
        string to,
        string subject,
        string htmlBody,
        EmailSendContext? context,
        EmailLogStatus status,
        string? errorMessage)
    {
        try
        {
            await _emailLogService.CreateAsync(new EmailLogCreateModel
            {
                ToEmail = to,
                Subject = subject,
                HtmlBody = htmlBody,
                TemplateKey = context?.TemplateKey,
                TemplateDisplayName = context?.TemplateDisplayName,
                Status = status,
                ErrorMessage = errorMessage,
                InitiatedByUserId = context?.InitiatedByUserId,
                InitiatedByEmail = context?.InitiatedByEmail,
                InitiatedByDisplayName = context?.InitiatedByDisplayName
            });
        }
        catch (Exception logEx)
        {
            _logger.LogWarning(logEx, "Failed to persist email log for recipient {Email}.", to);
        }
    }
}
