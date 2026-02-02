using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.ViewModels.Common;

namespace SAM.ViewModels.SystemAdmin;

/// <summary>
/// ViewModel for the Sprayfields partial view.
/// </summary>
public class SprayfieldsPartialViewModel
{
    public IEnumerable<SprayfieldViewModel> Sprayfields { get; set; } = new List<SprayfieldViewModel>();
    public FilterViewModel? Filter { get; set; }
    public bool IsGlobalAdmin { get; set; }
    public SelectList? Companies { get; set; }
    public Guid? SelectedCompanyId { get; set; }
}

