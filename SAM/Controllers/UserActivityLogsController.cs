using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using SAM.Controllers.Base;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Authorization;
using SAM.Services.Interfaces;
using SAM.Services.Models;
using SAM.ViewModels.UserActivityLogs;

namespace SAM.Controllers;

[Authorize(Policy = Policies.RequireAdmin)]
public class UserActivityLogsController : BaseController
{
    private readonly IUserActivityLogService _userActivityLogService;
    private readonly ICompanyService _companyService;

    public UserActivityLogsController(
        IUserActivityLogService userActivityLogService,
        ICompanyService companyService,
        UserManager<ApplicationUser> userManager,
        ILogger<UserActivityLogsController> logger)
        : base(userManager, logger)
    {
        _userActivityLogService = userActivityLogService;
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] UserActivityLogFilterViewModel filter)
    {
        // Global admin logs are always all-company scope.
        filter.CompanyId = null;

        var result = await _userActivityLogService.QueryAsync(new UserActivityLogQueryModel
        {
            CompanyId = filter.CompanyId,
            ActorUserId = filter.ActorUserId,
            ActivityType = filter.ActivityType,
            Module = filter.Module,
            FromUtc = filter.FromUtc,
            ToUtc = filter.ToUtc,
            SearchTerm = filter.SearchTerm,
            Page = filter.Page,
            PageSize = filter.PageSize
        });

        var companies = await _companyService.GetAllAsync();
        var companyMap = companies.ToDictionary(c => c.Id, c => c.Name);

        var logs = result.Items.Select(x => new UserActivityLogItemViewModel
        {
            Id = x.Id,
            OccurredAtUtc = x.OccurredAtUtc,
            ActivityType = x.ActivityType,
            Module = x.Module,
            EntityName = x.EntityName,
            EntityId = x.EntityId,
            CompanyId = x.CompanyId,
            CompanyName = x.CompanyId.HasValue && companyMap.TryGetValue(x.CompanyId.Value, out var name) ? name : "N/A",
            ActorEmail = x.ActorEmail,
            ActorDisplayName = x.ActorDisplayName,
            Summary = x.Summary,
            ChangedFields = ParseChangedFields(x.ChangedFields)
        }).ToList();

        var actors = await _userActivityLogService.GetActorsAsync(filter.CompanyId);
        var actorOptions = actors
            .Select(x => new SelectListItem
            {
                Value = x.Id,
                Text = string.IsNullOrWhiteSpace(x.FullName) ? (x.Email ?? x.Id) : $"{x.FullName} ({x.Email})"
            })
            .ToList();

        var modules = (await _userActivityLogService.GetModulesAsync(filter.CompanyId))
            .Select(x => new SelectListItem(x, x))
            .ToList();

        var activityTypes = Enum.GetValues(typeof(UserActivityType))
            .Cast<UserActivityType>()
            .Select(x => new SelectListItem(x.ToString(), x.ToString()))
            .ToList();

        var viewModel = new UserActivityLogsIndexViewModel
        {
            Filter = filter,
            Logs = new PagedResult<UserActivityLogItemViewModel>
            {
                Items = logs,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            },
            Actors = new SelectList(actorOptions, "Value", "Text", filter.ActorUserId),
            Modules = new SelectList(modules, "Value", "Text", filter.Module),
            ActivityTypes = new SelectList(activityTypes, "Value", "Text", filter.ActivityType?.ToString())
        };

        return View(viewModel);
    }

    private static IReadOnlyList<string> ParseChangedFields(string? changedFieldsJson)
    {
        if (string.IsNullOrWhiteSpace(changedFieldsJson))
        {
            return Array.Empty<string>();
        }

        try
        {
            var values = JsonSerializer.Deserialize<List<string>>(changedFieldsJson);
            return values ?? new List<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
