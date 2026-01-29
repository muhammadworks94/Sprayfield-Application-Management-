using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.SystemAdmin;

public class CompanyDeleteViewModel
{
    public Guid Id { get; set; }
    
    [Display(Name = "Company Name")]
    public string Name { get; set; } = string.Empty;
    
    [Display(Name = "Contact Email")]
    public string ContactEmail { get; set; } = string.Empty;
    
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Display(Name = "Website")]
    public string? Website { get; set; }
    
    [Display(Name = "Description")]
    public string? Description { get; set; }
    
    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; }
    
    // Related entity counts
    public int UsersCount { get; set; }
    public int FacilitiesCount { get; set; }
    public int SoilsCount { get; set; }
    public int CropsCount { get; set; }
    public int NozzlesCount { get; set; }
    public int SprayfieldsCount { get; set; }
    public int MonitoringWellsCount { get; set; }
    public int UserRequestsCount { get; set; }
    
    // Lists for detailed display
    public List<UserInfo> Users { get; set; } = new List<UserInfo>();
    public List<FacilityInfo> Facilities { get; set; } = new List<FacilityInfo>();
    public List<UserRequestInfo> UserRequests { get; set; } = new List<UserRequestInfo>();
    
    public int TotalRelatedItems => UsersCount + FacilitiesCount + SoilsCount + CropsCount + 
                                   NozzlesCount + SprayfieldsCount + MonitoringWellsCount + UserRequestsCount;
}

public class UserInfo
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class FacilityInfo
{
    public string Name { get; set; } = string.Empty;
    public string PermitNumber { get; set; } = string.Empty;
}

public class UserRequestInfo
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

