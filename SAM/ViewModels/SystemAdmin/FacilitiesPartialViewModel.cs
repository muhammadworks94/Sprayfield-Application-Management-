using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.ViewModels.Common;

namespace SAM.ViewModels.SystemAdmin;

/// <summary>
/// ViewModel for the Facilities partial view.
/// </summary>
public class FacilitiesPartialViewModel
{
    public IEnumerable<FacilityViewModel> Facilities { get; set; } = new List<FacilityViewModel>();
    public FilterViewModel? Filter { get; set; }
    public bool IsGlobalAdmin { get; set; }
    public SelectList? Companies { get; set; }
    public Guid? SelectedCompanyId { get; set; }
}

