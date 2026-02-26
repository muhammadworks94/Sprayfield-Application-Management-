using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Services.Models;

namespace SAM.ViewModels.UserActivityLogs;

public class UserActivityLogsIndexViewModel
{
    public UserActivityLogFilterViewModel Filter { get; set; } = new();
    public PagedResult<UserActivityLogItemViewModel> Logs { get; set; } = new();
    public SelectList Companies { get; set; } = new(Enumerable.Empty<SelectListItem>(), "Value", "Text");
    public SelectList Actors { get; set; } = new(Enumerable.Empty<SelectListItem>(), "Value", "Text");
    public SelectList ActivityTypes { get; set; } = new(Enumerable.Empty<SelectListItem>(), "Value", "Text");
    public SelectList Modules { get; set; } = new(Enumerable.Empty<SelectListItem>(), "Value", "Text");
}

