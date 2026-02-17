using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;
using SAM.Services.Models;

namespace SAM.Services.Implementations;

/// <summary>
/// Database-backed implementation for system email template management and rendering.
/// </summary>
public class EmailTemplateService : IEmailTemplateService
{
    private const string EmergencySubject = "SAM Notification";
    private const string EmergencyBody = "<p>This is an automated message from SAM.</p>";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailTemplateService> _logger;

    public EmailTemplateService(ApplicationDbContext context, ILogger<EmailTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EmailTemplate>> GetSystemTemplatesAsync()
    {
        await EnsureSystemTemplatesAsync();

        var templates = await _context.EmailTemplates
            .Where(t => t.IsSystemTemplate && EmailTemplateCatalog.SystemKeys.Contains(t.TemplateKey))
            .ToListAsync();

        var orderedTemplates = templates
            .OrderBy(t => EmailTemplateCatalog.GetOrder(t.TemplateKey))
            .ToList();

        return orderedTemplates;
    }

    public async Task<EmailTemplate?> GetByKeyAsync(string templateKey)
    {
        return await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.TemplateKey == templateKey);
    }

    public async Task UpdateSystemTemplateAsync(string templateKey, string subjectTemplate, string bodyTemplate)
    {
        await EnsureSystemTemplatesAsync();

        if (!EmailTemplateCatalog.IsSystemKey(templateKey))
        {
            throw new BusinessRuleException("Only predefined system templates can be updated.");
        }

        var normalizedSubject = (subjectTemplate ?? string.Empty).Trim();
        var normalizedBody = (bodyTemplate ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(normalizedSubject))
        {
            throw new BusinessRuleException("Email subject template is required.");
        }

        if (string.IsNullOrWhiteSpace(normalizedBody))
        {
            throw new BusinessRuleException("Email body template is required.");
        }

        if (normalizedSubject.Length > 500)
        {
            throw new BusinessRuleException("Email subject template cannot exceed 500 characters.");
        }

        if (normalizedBody.Length > 8000)
        {
            throw new BusinessRuleException("Email body template cannot exceed 8000 characters.");
        }

        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.TemplateKey == templateKey);

        if (template == null || !template.IsSystemTemplate)
        {
            throw new BusinessRuleException("Template not found or not editable.");
        }

        template.SubjectTemplate = normalizedSubject;
        template.BodyTemplate = normalizedBody;

        await _context.SaveChangesAsync();
    }

    public async Task<RenderedEmail> RenderAsync(string templateKey, IReadOnlyDictionary<string, string> tokens)
    {
        try
        {
            await EnsureSystemTemplatesAsync();

            var template = await _context.EmailTemplates
                .FirstOrDefaultAsync(t => t.TemplateKey == templateKey);

            if (template == null)
            {
                return EmergencyFallback(templateKey, "Template not found.");
            }

            if (!template.IsActive)
            {
                return EmergencyFallback(templateKey, "Template is inactive.");
            }

            var subject = template.SubjectTemplate;
            var body = template.BodyTemplate;

            foreach (var token in tokens)
            {
                var placeholder = $"{{{{{token.Key}}}}}";
                subject = subject.Replace(placeholder, token.Value ?? string.Empty, StringComparison.Ordinal);
                body = body.Replace(placeholder, token.Value ?? string.Empty, StringComparison.Ordinal);
            }

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
            {
                return EmergencyFallback(templateKey, "Rendered content is empty.");
            }

            return new RenderedEmail
            {
                Subject = subject,
                HtmlBody = body,
                UsedEmergencyFallback = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed rendering email template {TemplateKey}. Using emergency fallback.", templateKey);
            return new RenderedEmail
            {
                Subject = EmergencySubject,
                HtmlBody = EmergencyBody,
                UsedEmergencyFallback = true
            };
        }
    }

    private RenderedEmail EmergencyFallback(string templateKey, string reason)
    {
        _logger.LogWarning("Using emergency fallback for template {TemplateKey}. Reason: {Reason}", templateKey, reason);
        return new RenderedEmail
        {
            Subject = EmergencySubject,
            HtmlBody = EmergencyBody,
            UsedEmergencyFallback = true
        };
    }

    private async Task EnsureSystemTemplatesAsync()
    {
        var existingKeys = await _context.EmailTemplates
            .Where(t => t.IsSystemTemplate)
            .Select(t => t.TemplateKey)
            .ToListAsync();

        var missingKeys = EmailTemplateCatalog.SystemKeys
            .Where(k => !existingKeys.Contains(k))
            .ToList();

        if (!missingKeys.Any())
        {
            return;
        }

        var defaults = BuildDefaultTemplates()
            .Where(t => missingKeys.Contains(t.TemplateKey))
            .ToList();

        _context.EmailTemplates.AddRange(defaults);
        await _context.SaveChangesAsync();
    }

    private static IEnumerable<EmailTemplate> BuildDefaultTemplates()
    {
        yield return new EmailTemplate
        {
            TemplateKey = EmailTemplateCatalog.PasswordReset,
            DisplayName = "Password Reset",
            Description = "Sent when a user requests a password reset.",
            SubjectTemplate = "Reset your {{AppName}} password",
            BodyTemplate =
                "<div style=\"margin:0;padding:0;background-color:#f3f4f6;font-family:Arial,Helvetica,sans-serif;\">" +
                "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"width:100%;padding:24px 0;background-color:#f3f4f6;\">" +
                "<tr><td align=\"center\">" +
                "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"width:100%;max-width:640px;background-color:#ffffff;border:1px solid #e5e7eb;border-radius:10px;overflow:hidden;\">" +
                "<tr><td style=\"padding:20px 24px;background-color:#1d4ed8;color:#ffffff;\"><h2 style=\"margin:0;font-size:20px;line-height:1.3;\">{{AppName}} Password Reset</h2></td></tr>" +
                "<tr><td style=\"padding:24px;color:#111827;font-size:14px;line-height:1.6;\">" +
                "<p style=\"margin:0 0 14px;\">Hi {{RecipientEmail}},</p>" +
                "<p style=\"margin:0 0 14px;\">We received a request to reset your {{AppName}} account password.</p>" +
                "<p style=\"margin:0 0 20px;\">Click the button below to continue:</p>" +
                "<p style=\"margin:0 0 20px;\"><a href=\"{{ResetLink}}\" style=\"display:inline-block;padding:11px 18px;background-color:#1d4ed8;color:#ffffff;text-decoration:none;border-radius:6px;font-weight:600;\">Reset Password</a></p>" +
                "<p style=\"margin:0 0 10px;color:#6b7280;\">If the button does not work, copy and paste this URL into your browser:</p>" +
                "<p style=\"margin:0 0 16px;word-break:break-all;\"><a href=\"{{ResetLink}}\" style=\"color:#2563eb;\">{{ResetLink}}</a></p>" +
                "<p style=\"margin:0;color:#6b7280;\">If you did not request this, you can ignore this email.</p>" +
                "</td></tr>" +
                "<tr><td style=\"padding:14px 24px;background-color:#f9fafb;border-top:1px solid #e5e7eb;color:#6b7280;font-size:12px;\">This is an automated message from {{AppName}}. Please do not reply.</td></tr>" +
                "</table></td></tr></table></div>",
            IsSystemTemplate = true,
            IsActive = true
        };

        yield return new EmailTemplate
        {
            TemplateKey = EmailTemplateCatalog.UserCredentials,
            DisplayName = "User Credentials",
            Description = "Sent when a new user account is created with a temporary password.",
            SubjectTemplate = "Your {{AppName}} account credentials",
            BodyTemplate =
                "<div style=\"margin:0;padding:0;background-color:#f3f4f6;font-family:Arial,Helvetica,sans-serif;\">" +
                "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"width:100%;padding:24px 0;background-color:#f3f4f6;\">" +
                "<tr><td align=\"center\">" +
                "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"width:100%;max-width:640px;background-color:#ffffff;border:1px solid #e5e7eb;border-radius:10px;overflow:hidden;\">" +
                "<tr><td style=\"padding:20px 24px;background-color:#1d4ed8;color:#ffffff;\"><h2 style=\"margin:0;font-size:20px;line-height:1.3;\">Welcome to {{AppName}}</h2></td></tr>" +
                "<tr><td style=\"padding:24px;color:#111827;font-size:14px;line-height:1.6;\">" +
                "<p style=\"margin:0 0 14px;\">Your account has been created successfully.</p>" +
                "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"width:100%;border:1px solid #e5e7eb;border-radius:8px;background-color:#f9fafb;margin:0 0 16px;\">" +
                "<tr><td style=\"padding:12px 14px;border-bottom:1px solid #e5e7eb;\"><strong>Email:</strong> {{RecipientEmail}}</td></tr>" +
                "<tr><td style=\"padding:12px 14px;border-bottom:1px solid #e5e7eb;\"><strong>Temporary Password:</strong> <span style=\"font-family:Consolas,Monaco,monospace;color:#b91c1c;font-weight:700;\">{{TemporaryPassword}}</span></td></tr>" +
                "<tr><td style=\"padding:12px 14px;border-bottom:1px solid #e5e7eb;\"><strong>Company:</strong> {{CompanyName}}</td></tr>" +
                "<tr><td style=\"padding:12px 14px;\"><strong>Role:</strong> {{RoleName}}</td></tr>" +
                "</table>" +
                "<p style=\"margin:0 0 12px;padding:12px;background-color:#fffbeb;border-left:4px solid #f59e0b;color:#92400e;\">For security, change your password immediately after first login.</p>" +
                "<p style=\"margin:0;color:#6b7280;\">If you were not expecting this account, contact your administrator.</p>" +
                "</td></tr>" +
                "<tr><td style=\"padding:14px 24px;background-color:#f9fafb;border-top:1px solid #e5e7eb;color:#6b7280;font-size:12px;\">This is an automated message from {{AppName}}. Please do not reply.</td></tr>" +
                "</table></td></tr></table></div>",
            IsSystemTemplate = true,
            IsActive = true
        };

        yield return new EmailTemplate
        {
            TemplateKey = EmailTemplateCatalog.SmtpTest,
            DisplayName = "SMTP Test Email",
            Description = "Sent when an admin tests SMTP delivery from system settings.",
            SubjectTemplate = "{{AppName}} SMTP test email",
            BodyTemplate =
                "<div style=\"margin:0;padding:0;background-color:#f3f4f6;font-family:Arial,Helvetica,sans-serif;\">" +
                "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"width:100%;padding:24px 0;background-color:#f3f4f6;\">" +
                "<tr><td align=\"center\">" +
                "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"width:100%;max-width:640px;background-color:#ffffff;border:1px solid #e5e7eb;border-radius:10px;overflow:hidden;\">" +
                "<tr><td style=\"padding:20px 24px;background-color:#1d4ed8;color:#ffffff;\"><h2 style=\"margin:0;font-size:20px;line-height:1.3;\">{{AppName}} SMTP Test</h2></td></tr>" +
                "<tr><td style=\"padding:24px;color:#111827;font-size:14px;line-height:1.6;\">" +
                "<p style=\"margin:0 0 14px;\">This is a test message from {{AppName}} System Settings.</p>" +
                "<p style=\"margin:0 0 8px;\"><strong>Recipient:</strong> {{RecipientEmail}}</p>" +
                "<p style=\"margin:0;\"><strong>Sent At (UTC):</strong> {{SentAtUtc}}</p>" +
                "</td></tr>" +
                "<tr><td style=\"padding:14px 24px;background-color:#f9fafb;border-top:1px solid #e5e7eb;color:#6b7280;font-size:12px;\">SMTP connectivity test completed by {{AppName}}.</td></tr>" +
                "</table></td></tr></table></div>",
            IsSystemTemplate = true,
            IsActive = true
        };
    }
}
