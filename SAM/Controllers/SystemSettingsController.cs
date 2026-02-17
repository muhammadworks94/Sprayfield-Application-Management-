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
    private readonly IEmailTemplateService _emailTemplateService;

    public SystemSettingsController(
        ISmtpSettingsService smtpSettingsService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        UserManager<ApplicationUser> userManager,
        ILogger<SystemSettingsController> logger)
        : base(userManager, logger)
    {
        _smtpSettingsService = smtpSettingsService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
    }

    [HttpGet]
    public async Task<IActionResult> Settings(string? templateKey = null)
    {
        var viewModel = await BuildSettingsPageViewModelAsync(templateKey);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings([Bind(Prefix = "Smtp")] SmtpSettingsViewModel smtpViewModel, string? templateKey = null)
    {
        if (!ModelState.IsValid)
        {
            var pageModel = await BuildSettingsPageViewModelAsync(templateKey, smtpOverride: smtpViewModel);
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
            return RedirectToAction(nameof(Settings), new { templateKey });
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var pageModel = await BuildSettingsPageViewModelAsync(templateKey, smtpOverride: smtpViewModel);
            return View(pageModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTestEmail(string testEmail, string? templateKey = null)
    {
        if (string.IsNullOrWhiteSpace(testEmail))
        {
            TempData["ErrorMessage"] = "Please enter an email address to send a test email.";
            return RedirectToAction(nameof(Settings), new { templateKey });
        }

        var emailValidator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
        if (!emailValidator.IsValid(testEmail))
        {
            TempData["ErrorMessage"] = "Please enter a valid email address.";
            return RedirectToAction(nameof(Settings), new { templateKey });
        }

        try
        {
            var trimmedEmail = testEmail.Trim();
            var renderedEmail = await _emailTemplateService.RenderAsync(
                EmailTemplateCatalog.SmtpTest,
                new Dictionary<string, string>
                {
                    ["AppName"] = EmailTemplateCatalog.AppName,
                    ["RecipientEmail"] = trimmedEmail,
                    ["SentAtUtc"] = DateTime.UtcNow.ToString("u")
                });

            await _emailService.SendEmailAsync(
                trimmedEmail,
                renderedEmail.Subject,
                renderedEmail.HtmlBody);

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

        return RedirectToAction(nameof(Settings), new { templateKey });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEmailTemplate([Bind(Prefix = "TemplateEditor")] EmailTemplateEditorViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var pageModel = await BuildSettingsPageViewModelAsync(model.TemplateKey, templateEditorOverride: model);
            return View(nameof(Settings), pageModel);
        }

        try
        {
            await _emailTemplateService.UpdateSystemTemplateAsync(model.TemplateKey, model.SubjectTemplate, model.BodyTemplate);
            TempData["SuccessMessage"] = "Email template updated successfully.";
            return RedirectToAction(nameof(Settings), new { templateKey = model.TemplateKey });
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var pageModel = await BuildSettingsPageViewModelAsync(model.TemplateKey, templateEditorOverride: model);
            return View(nameof(Settings), pageModel);
        }
    }

    private async Task<SystemSettingsPageViewModel> BuildSettingsPageViewModelAsync(
        string? selectedTemplateKey = null,
        SmtpSettingsViewModel? smtpOverride = null,
        EmailTemplateEditorViewModel? templateEditorOverride = null)
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

        var templates = await _emailTemplateService.GetSystemTemplatesAsync();
        var effectiveTemplateKey = !string.IsNullOrWhiteSpace(selectedTemplateKey)
            ? selectedTemplateKey
            : templates.FirstOrDefault()?.TemplateKey ?? EmailTemplateCatalog.PasswordReset;

        var selectedTemplate = templates.FirstOrDefault(t => t.TemplateKey == effectiveTemplateKey);
        var templateEditor = templateEditorOverride ?? new EmailTemplateEditorViewModel
        {
            TemplateKey = selectedTemplate?.TemplateKey ?? effectiveTemplateKey,
            DisplayName = selectedTemplate?.DisplayName ?? "Email Template",
            Description = selectedTemplate?.Description ?? "Template details are unavailable.",
            SubjectTemplate = selectedTemplate?.SubjectTemplate ?? string.Empty,
            BodyTemplate = selectedTemplate?.BodyTemplate ?? string.Empty,
            AllowedTokens = EmailTemplateCatalog.GetAllowedTokens(effectiveTemplateKey)
        };

        // Ensure token help reflects the selected template on failed postbacks too.
        templateEditor.AllowedTokens = EmailTemplateCatalog.GetAllowedTokens(templateEditor.TemplateKey);

        if (selectedTemplate != null)
        {
            templateEditor.DisplayName = selectedTemplate.DisplayName;
            templateEditor.Description = selectedTemplate.Description;
        }

        return new SystemSettingsPageViewModel
        {
            Smtp = smtpViewModel,
            TemplateEditor = templateEditor,
            Templates = templates.Select(t => new EmailTemplateSummaryViewModel
            {
                TemplateKey = t.TemplateKey,
                DisplayName = t.DisplayName,
                Description = t.Description
            }).ToList()
        };
    }
}
