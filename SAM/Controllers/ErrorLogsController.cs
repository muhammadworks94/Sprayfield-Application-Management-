using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Controllers.Base;
using SAM.Domain.Entities;
using SAM.Infrastructure.Authorization;
using SAM.Services.Interfaces;
using SAM.Services.Models;
using SAM.ViewModels.ErrorLogs;

namespace SAM.Controllers;

[Authorize(Policy = Policies.RequireAdmin)]
public class ErrorLogsController : BaseController
{
    private readonly IErrorLogService _errorLogService;
    private readonly ICompanyService _companyService;

    public ErrorLogsController(
        IErrorLogService errorLogService,
        ICompanyService companyService,
        UserManager<ApplicationUser> userManager,
        ILogger<ErrorLogsController> logger)
        : base(userManager, logger)
    {
        _errorLogService = errorLogService;
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ErrorLogFilterViewModel filter, CancellationToken cancellationToken)
    {
        // Global admin logs are always all-company scope.
        filter.CompanyId = null;

        var result = await _errorLogService.QueryAsync(new ErrorLogQueryModel
        {
            CompanyId = filter.CompanyId,
            ActorUserId = filter.ActorUserId,
            Module = filter.Module,
            ExceptionType = filter.ExceptionType,
            StatusCode = filter.StatusCode,
            FromUtc = filter.FromUtc,
            ToUtc = filter.ToUtc,
            SearchTerm = filter.SearchTerm,
            Page = filter.Page,
            PageSize = filter.PageSize
        }, cancellationToken);

        var companies = await _companyService.GetAllAsync();
        var companyMap = companies.ToDictionary(c => c.Id, c => c.Name);

        var logs = result.Items.Select(x => new ErrorLogItemViewModel
        {
            Id = x.Id,
            OccurredAtUtc = x.OccurredAtUtc,
            ExceptionType = x.ExceptionType,
            Message = x.Message,
            StatusCode = x.StatusCode,
            Module = x.Module,
            Path = x.Path,
            RequestId = x.RequestId,
            ActorDisplayName = x.ActorDisplayName,
            ActorEmail = x.ActorEmail,
            CompanyId = x.CompanyId,
            CompanyName = x.CompanyId.HasValue && companyMap.TryGetValue(x.CompanyId.Value, out var name) ? name : "N/A"
        }).ToList();

        var actors = await _errorLogService.GetActorsAsync(filter.CompanyId, cancellationToken);
        var actorOptions = actors.Select(x => new SelectListItem
        {
            Value = x.Id,
            Text = string.IsNullOrWhiteSpace(x.FullName) ? (x.Email ?? x.Id) : $"{x.FullName} ({x.Email})"
        }).ToList();

        var modules = (await _errorLogService.GetModulesAsync(filter.CompanyId, cancellationToken))
            .Select(x => new SelectListItem(x, x))
            .ToList();

        var exceptionTypes = (await _errorLogService.GetExceptionTypesAsync(filter.CompanyId, cancellationToken))
            .Select(x => new SelectListItem(x, x))
            .ToList();

        var statusCodes = (await _errorLogService.GetStatusCodesAsync(filter.CompanyId, cancellationToken))
            .Select(x => new SelectListItem(x.ToString(), x.ToString()))
            .ToList();

        return View(new ErrorLogsIndexViewModel
        {
            Filter = filter,
            Logs = new PagedResult<ErrorLogItemViewModel>
            {
                Items = logs,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            },
            Actors = new SelectList(actorOptions, "Value", "Text", filter.ActorUserId),
            Modules = new SelectList(modules, "Value", "Text", filter.Module),
            ExceptionTypes = new SelectList(exceptionTypes, "Value", "Text", filter.ExceptionType),
            StatusCodes = new SelectList(statusCodes, "Value", "Text", filter.StatusCode?.ToString())
        });
    }
}
