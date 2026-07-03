using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SAM.Controllers.Base;
using SAM.Domain.Entities;
using SAM.Infrastructure.Authorization;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;
using SAM.Services.Models;
using SAM.ViewModels.EmailTemplates;

namespace SAM.Controllers;

[Authorize(Policy = Policies.RequireAdmin)]
public class EmailTemplatesController : BaseController
{
    private readonly IEmailTemplateService _emailTemplateService;

    public EmailTemplatesController(
        IEmailTemplateService emailTemplateService,
        UserManager<ApplicationUser> userManager,
        ILogger<EmailTemplatesController> logger)
        : base(userManager, logger)
    {
        _emailTemplateService = emailTemplateService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? templateKey = null)
    {
        var viewModel = await BuildPageViewModelAsync(templateKey);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTemplate([Bind(Prefix = "TemplateEditor")] EmailTemplateEditorViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var pageModel = await BuildPageViewModelAsync(model.TemplateKey, templateEditorOverride: model);
            return View(nameof(Index), pageModel);
        }

        try
        {
            await _emailTemplateService.UpdateSystemTemplateAsync(model.TemplateKey, model.SubjectTemplate, model.BodyTemplate);
            TempData["SuccessMessage"] = "Email template updated successfully.";
            return RedirectToAction(nameof(Index), new { templateKey = model.TemplateKey });
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var pageModel = await BuildPageViewModelAsync(model.TemplateKey, templateEditorOverride: model);
            return View(nameof(Index), pageModel);
        }
    }

    private async Task<EmailTemplatesPageViewModel> BuildPageViewModelAsync(
        string? selectedTemplateKey = null,
        EmailTemplateEditorViewModel? templateEditorOverride = null)
    {
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

        templateEditor.AllowedTokens = EmailTemplateCatalog.GetAllowedTokens(templateEditor.TemplateKey);

        if (selectedTemplate != null)
        {
            templateEditor.DisplayName = selectedTemplate.DisplayName;
            templateEditor.Description = selectedTemplate.Description;
        }

        return new EmailTemplatesPageViewModel
        {
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
