using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.ViewModels.Common;

namespace SAM.ViewModels.SystemAdmin;

/// <summary>
/// ViewModel for the Nozzles partial view.
/// </summary>
public class NozzlesPartialViewModel
{
    public IEnumerable<NozzleViewModel> Nozzles { get; set; } = new List<NozzleViewModel>();
    public FilterViewModel? Filter { get; set; }
    public bool IsGlobalAdmin { get; set; }
    public SelectList? Companies { get; set; }
    public Guid? SelectedCompanyId { get; set; }
}

