using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.ViewModels.Common;

namespace SAM.ViewModels.SystemAdmin;

/// <summary>
/// ViewModel for the MonitoringWells partial view.
/// </summary>
public class MonitoringWellsPartialViewModel
{
    public IEnumerable<MonitoringWellViewModel> MonitoringWells { get; set; } = new List<MonitoringWellViewModel>();
    public FilterViewModel? Filter { get; set; }
    public bool IsGlobalAdmin { get; set; }
    public SelectList? Companies { get; set; }
    public Guid? SelectedCompanyId { get; set; }
}

