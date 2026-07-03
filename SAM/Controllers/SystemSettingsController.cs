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
    private readonly IEmailTemplateService _emailTemplateService;

    public SystemSettingsController(
        ISmtpSettingsService smtpSettingsService,
        IEmailTemplateService emailTemplateService,
        UserManager<ApplicationUser> userManager,
        ILogger<SystemSettingsController> logger)
        : base(userManager, logger)
    {
        _smtpSettingsService = smtpSettingsService;
        _emailTemplateService = emailTemplateService;
    }

    [HttpGet]
    public async Task<IActionResult> Settings()
    {
        var viewModel = await BuildSettingsPageViewModelAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings([Bind(Prefix = "Smtp")] SmtpSettingsViewModel smtpViewModel)
    {
        if (!ModelState.IsValid)
        {
            var pageModel = await BuildSettingsPageViewModelAsync(smtpOverride: smtpViewModel);
            return View(pageModel);
        }

        try
        {
            await _smtpSettingsService.UpsertGlobalAsync(new SmtpSettingsUpdateModel
            {
                Host = smtpViewModel.Host,
                Port = smtpViewModel.Port,
                Username = smtpViewModel.Username,
                Password = smtpViewModel.Password,
                FromEmail = smtpViewModel.FromEmail,
                EnableSsl = smtpViewModel.EnableSsl
            });

            TempData["SuccessMessage"] = "SMTP settings updated successfully.";
            return RedirectToAction(nameof(Settings));
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var pageModel = await BuildSettingsPageViewModelAsync(smtpOverride: smtpViewModel);
            return View(pageModel);
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
            var trimmedEmail = testEmail.Trim();
            var currentUser = await UserManager.GetUserAsync(User);
            await _emailTemplateService.SendTemplatedEmailAsync(
                trimmedEmail,
                EmailTemplateCatalog.SmtpTest,
                new Dictionary<string, string>
                {
                    ["AppName"] = EmailTemplateCatalog.AppName,
                    ["RecipientEmail"] = trimmedEmail,
                    ["SentAtUtc"] = DateTime.UtcNow.ToString("u")
                },
                new EmailSendContext
                {
                    InitiatedByUserId = currentUser?.Id,
                    InitiatedByEmail = currentUser?.Email,
                    InitiatedByDisplayName = currentUser?.FullName
                });

            TempData["SuccessMessage"] = $"Test email sent successfully to {trimmedEmail}.";
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

    private async Task<SystemSettingsPageViewModel> BuildSettingsPageViewModelAsync(
        SmtpSettingsViewModel? smtpOverride = null)
    {
        var settings = await _smtpSettingsService.GetGlobalAsync();
        var smtpViewModel = smtpOverride ?? new SmtpSettingsViewModel
        {
            Host = settings?.Host ?? string.Empty,
            Port = settings?.Port ?? 587,
            Username = settings?.Username,
            FromEmail = settings?.FromEmail ?? string.Empty,
            EnableSsl = settings?.EnableSsl ?? true
        };

        smtpViewModel.HasPasswordConfigured = settings != null && !string.IsNullOrWhiteSpace(settings.PasswordEncrypted);

        return new SystemSettingsPageViewModel
        {
            Smtp = smtpViewModel
        };
    }
}
