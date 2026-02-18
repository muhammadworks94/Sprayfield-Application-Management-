using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Services.Models;

namespace SAM.ViewModels.ErrorLogs;

public class ErrorLogsIndexViewModel
{
    public ErrorLogFilterViewModel Filter { get; set; } = new();
    public PagedResult<ErrorLogItemViewModel> Logs { get; set; } = new();
    public SelectList Actors { get; set; } = new(Enumerable.Empty<SelectListItem>(), "Value", "Text");
    public SelectList Modules { get; set; } = new(Enumerable.Empty<SelectListItem>(), "Value", "Text");
    public SelectList ExceptionTypes { get; set; } = new(Enumerable.Empty<SelectListItem>(), "Value", "Text");
    public SelectList StatusCodes { get; set; } = new(Enumerable.Empty<SelectListItem>(), "Value", "Text");
}

