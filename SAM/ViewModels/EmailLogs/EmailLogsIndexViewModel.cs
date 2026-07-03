using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Services.Models;

namespace SAM.ViewModels.EmailLogs;

public class EmailLogsIndexViewModel
{
    public EmailLogFilterViewModel Filter { get; set; } = new();
    public PagedResult<EmailLogItemViewModel> Logs { get; set; } = new();
    public SelectList TemplateKeys { get; set; } = new(Enumerable.Empty<SelectListItem>(), "Value", "Text");
    public SelectList Statuses { get; set; } = new(Enumerable.Empty<SelectListItem>(), "Value", "Text");
}
