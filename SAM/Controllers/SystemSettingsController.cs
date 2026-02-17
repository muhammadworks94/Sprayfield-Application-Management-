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

    public SystemSettingsController(
        ISmtpSettingsService smtpSettingsService,
        UserManager<ApplicationUser> userManager,
        ILogger<SystemSettingsController> logger)
        : base(userManager, logger)
    {
        _smtpSettingsService = smtpSettingsService;
    }

    [HttpGet]
    public async Task<IActionResult> Email()
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
    public async Task<IActionResult> Email(SmtpSettingsViewModel viewModel)
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
            return RedirectToAction(nameof(Email));
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var existing = await _smtpSettingsService.GetGlobalAsync();
            viewModel.HasPasswordConfigured = existing != null && !string.IsNullOrWhiteSpace(existing.PasswordEncrypted);
            return View(viewModel);
        }
    }
}
