using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Controllers.Base;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Authorization;
using SAM.Services.Interfaces;
using SAM.Services.Models;
using SAM.ViewModels.EmailLogs;

namespace SAM.Controllers;

[Authorize(Policy = Policies.RequireAdmin)]
public class EmailLogsController : BaseController
{
    private readonly IEmailLogService _emailLogService;

    public EmailLogsController(
        IEmailLogService emailLogService,
        UserManager<ApplicationUser> userManager,
        ILogger<EmailLogsController> logger)
        : base(userManager, logger)
    {
        _emailLogService = emailLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] EmailLogFilterViewModel filter, CancellationToken cancellationToken)
    {
        var result = await _emailLogService.QueryAsync(new EmailLogQueryModel
        {
            TemplateKey = filter.TemplateKey,
            Status = filter.Status,
            FromUtc = filter.FromUtc,
            ToUtc = filter.ToUtc,
            SearchTerm = filter.SearchTerm,
            Page = filter.Page,
            PageSize = filter.PageSize
        }, cancellationToken);

        var logs = result.Items.Select(x => new EmailLogItemViewModel
        {
            Id = x.Id,
            SentAtUtc = x.SentAtUtc,
            ToEmail = x.ToEmail,
            Subject = x.Subject,
            TemplateKey = x.TemplateKey,
            TemplateDisplayName = x.TemplateDisplayName,
            Status = x.Status,
            ErrorMessage = x.ErrorMessage,
            HtmlBody = x.HtmlBody,
            InitiatedByDisplayName = x.InitiatedByDisplayName,
            InitiatedByEmail = x.InitiatedByEmail
        }).ToList();

        var templateKeys = await _emailLogService.GetTemplateKeysAsync(cancellationToken);
        var templateOptions = templateKeys.Select(x => new SelectListItem(x, x)).ToList();

        var statusOptions = new[]
        {
            new SelectListItem("Sent", EmailLogStatus.Sent.ToString()),
            new SelectListItem("Failed", EmailLogStatus.Failed.ToString())
        };

        return View(new EmailLogsIndexViewModel
        {
            Filter = filter,
            Logs = new PagedResult<EmailLogItemViewModel>
            {
                Items = logs,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            },
            TemplateKeys = new SelectList(templateOptions, "Value", "Text", filter.TemplateKey),
            Statuses = new SelectList(statusOptions, "Value", "Text", filter.Status?.ToString())
        });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(Guid id, CancellationToken cancellationToken)
    {
        var item = await _emailLogService.GetByIdAsync(id, cancellationToken);

        if (item == null)
        {
            return NotFound();
        }

        return Json(new
        {
            item.Subject,
            item.HtmlBody,
            item.ErrorMessage
        });
    }
}
