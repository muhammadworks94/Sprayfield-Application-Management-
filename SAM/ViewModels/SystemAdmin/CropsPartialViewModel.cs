using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.ViewModels.Common;

namespace SAM.ViewModels.SystemAdmin;

/// <summary>
/// ViewModel for the Crops partial view.
/// </summary>
public class CropsPartialViewModel
{
    public IEnumerable<CropViewModel> Crops { get; set; } = new List<CropViewModel>();
    public FilterViewModel? Filter { get; set; }
    public bool IsGlobalAdmin { get; set; }
    public SelectList? Companies { get; set; }
    public Guid? SelectedCompanyId { get; set; }
}

