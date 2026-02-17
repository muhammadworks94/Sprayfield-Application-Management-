using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;
using SAM.Services.Models;

namespace SAM.Services.Implementations;

/// <summary>
/// Manages global SMTP settings persisted in the database.
/// </summary>
public class SmtpSettingsService : ISmtpSettingsService
{
    private const string GlobalScopeKey = "SMTP_GLOBAL";
    private const string PasswordProtectionPurpose = "SAM.SmtpSettings.Password.v1";

    private readonly ApplicationDbContext _context;
    private readonly IDataProtector _protector;
    private readonly ILogger<SmtpSettingsService> _logger;

    public SmtpSettingsService(
        ApplicationDbContext context,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<SmtpSettingsService> logger)
    {
        _context = context;
        _protector = dataProtectionProvider.CreateProtector(PasswordProtectionPurpose);
        _logger = logger;
    }

    public async Task<SmtpSettings?> GetGlobalAsync()
    {
        return await _context.SmtpSettings
            .FirstOrDefaultAsync(s => s.ScopeKey == GlobalScopeKey);
    }

    public async Task UpsertGlobalAsync(SmtpSettingsUpdateModel input)
    {
        var existing = await GetGlobalAsync();
        var normalizedHost = input.Host.Trim();
        var normalizedFromEmail = input.FromEmail.Trim();
        var normalizedUsername = string.IsNullOrWhiteSpace(input.Username) ? null : input.Username.Trim();

        if (string.IsNullOrWhiteSpace(normalizedHost))
        {
            throw new BusinessRuleException("SMTP host is required.");
        }

        if (input.Port < 1 || input.Port > 65535)
        {
            throw new BusinessRuleException("SMTP port must be between 1 and 65535.");
        }

        if (string.IsNullOrWhiteSpace(normalizedFromEmail))
        {
            throw new BusinessRuleException("From email is required.");
        }

        if (existing == null)
        {
            if (string.IsNullOrWhiteSpace(input.Password))
            {
                throw new BusinessRuleException("SMTP password is required for first-time setup.");
            }

            existing = new SmtpSettings
            {
                ScopeKey = GlobalScopeKey
            };

            _context.SmtpSettings.Add(existing);
        }

        existing.Host = normalizedHost;
        existing.Port = input.Port;
        existing.Username = normalizedUsername;
        existing.FromEmail = normalizedFromEmail;
        existing.EnableSsl = input.EnableSsl;

        if (!string.IsNullOrWhiteSpace(input.Password))
        {
            existing.PasswordEncrypted = _protector.Protect(input.Password);
        }
        else if (string.IsNullOrWhiteSpace(existing.PasswordEncrypted))
        {
            throw new BusinessRuleException("SMTP password is required.");
        }

        await _context.SaveChangesAsync();
    }

    public async Task<SmtpRuntimeOptions> GetRuntimeOptionsAsync()
    {
        var settings = await GetGlobalAsync();
        if (settings == null)
        {
            throw new BusinessRuleException("SMTP settings are not configured. Please configure them in System Settings.");
        }

        if (string.IsNullOrWhiteSpace(settings.Host) ||
            settings.Port <= 0 ||
            string.IsNullOrWhiteSpace(settings.FromEmail) ||
            string.IsNullOrWhiteSpace(settings.PasswordEncrypted))
        {
            throw new BusinessRuleException("SMTP settings are incomplete. Please update them in System Settings.");
        }

        string decryptedPassword;
        try
        {
            decryptedPassword = _protector.Unprotect(settings.PasswordEncrypted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt SMTP password from database.");
            throw new BusinessRuleException("SMTP password could not be read. Please reset it in System Settings.");
        }

        return new SmtpRuntimeOptions
        {
            Host = settings.Host,
            Port = settings.Port,
            Username = settings.Username,
            Password = decryptedPassword,
            FromEmail = settings.FromEmail,
            EnableSsl = settings.EnableSsl
        };
    }
}
