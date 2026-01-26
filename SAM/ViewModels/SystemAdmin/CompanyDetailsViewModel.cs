using SAM.Domain.Entities;

namespace SAM.ViewModels.SystemAdmin;

public class CompanyDetailsViewModel
{
    // Company Information
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? Description { get; set; }
    public string? TaxId { get; set; }
    public string? FaxNumber { get; set; }
    public string? LicenseNumber { get; set; }
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    // Related Entities Collections
    public List<UserListItemViewModel> Users { get; set; } = new List<UserListItemViewModel>();
    public List<FacilityViewModel> Facilities { get; set; } = new List<FacilityViewModel>();
    public List<SoilViewModel> Soils { get; set; } = new List<SoilViewModel>();
    public List<CropViewModel> Crops { get; set; } = new List<CropViewModel>();
    public List<NozzleViewModel> Nozzles { get; set; } = new List<NozzleViewModel>();
    public List<SprayfieldViewModel> Sprayfields { get; set; } = new List<SprayfieldViewModel>();
    public List<MonitoringWellViewModel> MonitoringWells { get; set; } = new List<MonitoringWellViewModel>();

    // Counts
    public int UsersCount => Users.Count;
    public int FacilitiesCount => Facilities.Count;
    public int SoilsCount => Soils.Count;
    public int CropsCount => Crops.Count;
    public int NozzlesCount => Nozzles.Count;
    public int SprayfieldsCount => Sprayfields.Count;
    public int MonitoringWellsCount => MonitoringWells.Count;

    // Permission flags
    public bool IsGlobalAdmin { get; set; }
}

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
}

