using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.ViewModels.Common;

namespace SAM.ViewModels.SystemAdmin;

/// <summary>
/// Main ViewModel for the SystemAdmin tabbed interface.
/// Contains data for all tabs, but only the active tab's data is populated.
/// </summary>
public class SystemAdminViewModel
{
    public string ActiveTab { get; set; } = "facilities";
    public bool IsGlobalAdmin { get; set; }
    public Guid? SelectedCompanyId { get; set; }
    public SelectList? Companies { get; set; }
    
    // Data for each tab
    public IEnumerable<FacilityViewModel>? Facilities { get; set; }
    public IEnumerable<SoilViewModel>? Soils { get; set; }
    public IEnumerable<CropViewModel>? Crops { get; set; }
    public IEnumerable<NozzleViewModel>? Nozzles { get; set; }
    public IEnumerable<SprayfieldViewModel>? Sprayfields { get; set; }
    public IEnumerable<MonitoringWellViewModel>? MonitoringWells { get; set; }
    
    // Filter ViewModels for each tab
    public FilterViewModel? FacilitiesFilter { get; set; }
    public FilterViewModel? SoilsFilter { get; set; }
    public FilterViewModel? CropsFilter { get; set; }
    public FilterViewModel? NozzlesFilter { get; set; }
    public FilterViewModel? SprayfieldsFilter { get; set; }
    public FilterViewModel? MonitoringWellsFilter { get; set; }
}

