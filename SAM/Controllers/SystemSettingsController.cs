using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SAM.Controllers.Base;
using SAM.Domain.Entities;
using SAM.Infrastructure.Authorization;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;
using SAM.Services.Models;
using SAM.ViewModels.SystemSettings;

namespace SAM.Controllers;

/// <summary>
/// Controller for global system settings.
/// </summary>
[Authorize(Policy = Policies.RequireAdmin)]
public class SystemSettingsController : BaseController
{
    private readonly ISmtpSettingsService _smtpSettingsService;
    private readonly IEmailService _emailService;

    public SystemSettingsController(
        ISmtpSettingsService smtpSettingsService,
        IEmailService emailService,
        UserManager<ApplicationUser> userManager,
        ILogger<SystemSettingsController> logger)
        : base(userManager, logger)
    {
        _smtpSettingsService = smtpSettingsService;
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<IActionResult> Settings()
    {
        var settings = await _smtpSettingsService.GetGlobalAsync();

        var viewModel = new SmtpSettingsViewModel
        {
            Host = settings?.Host ?? string.Empty,
            Port = settings?.Port ?? 587,
            Username = settings?.Username,
            FromEmail = settings?.FromEmail ?? string.Empty,
            EnableSsl = settings?.EnableSsl ?? true,
            HasPasswordConfigured = settings != null && !string.IsNullOrWhiteSpace(settings.PasswordEncrypted)
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(SmtpSettingsViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            var existing = await _smtpSettingsService.GetGlobalAsync();
            viewModel.HasPasswordConfigured = existing != null && !string.IsNullOrWhiteSpace(existing.PasswordEncrypted);
            return View(viewModel);
        }

        try
        {
            await _smtpSettingsService.UpsertGlobalAsync(new SmtpSettingsUpdateModel
            {
                Host = viewModel.Host,
                Port = viewModel.Port,
                Username = viewModel.Username,
                Password = viewModel.Password,
                FromEmail = viewModel.FromEmail,
                EnableSsl = viewModel.EnableSsl
            });

            TempData["SuccessMessage"] = "SMTP settings updated successfully.";
            return RedirectToAction(nameof(Settings));
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var existing = await _smtpSettingsService.GetGlobalAsync();
            viewModel.HasPasswordConfigured = existing != null && !string.IsNullOrWhiteSpace(existing.PasswordEncrypted);
            return View(viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTestEmail(string testEmail)
    {
        if (string.IsNullOrWhiteSpace(testEmail))
        {
            TempData["ErrorMessage"] = "Please enter an email address to send a test email.";
            return RedirectToAction(nameof(Settings));
        }

        var emailValidator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
        if (!emailValidator.IsValid(testEmail))
        {
            TempData["ErrorMessage"] = "Please enter a valid email address.";
            return RedirectToAction(nameof(Settings));
        }

        try
        {
            await _emailService.SendEmailAsync(
                testEmail.Trim(),
                "SAM SMTP Test Email",
                "<p>This is a test email from SAM System Settings.</p><p>Your SMTP configuration is working.</p>");

            TempData["SuccessMessage"] = $"Test email sent successfully to {testEmail.Trim()}.";
        }
        catch (BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send SMTP test email to {Email}.", testEmail);
            TempData["ErrorMessage"] = "Failed to send test email. Please verify SMTP host, port, credentials, and SSL settings.";
        }

        return RedirectToAction(nameof(Settings));
    }
}
