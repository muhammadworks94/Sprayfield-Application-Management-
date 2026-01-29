using System.ComponentModel.DataAnnotations;
using SAM.Domain.Enums;

namespace SAM.ViewModels.UserManagement;

public class AdminRequestViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [Display(Name = "Request Type")]
    public AdminRequestTypeEnum RequestType { get; set; }
    
    [Required]
    [EmailAddress]
    [StringLength(256)]
    [Display(Name = "Target Email")]
    public string TargetEmail { get; set; } = string.Empty;
    
    [StringLength(200)]
    [Display(Name = "Full Name")]
    public string TargetFullName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(1000)]
    [Display(Name = "Justification")]
    public string Justification { get; set; } = string.Empty;
    
    [Display(Name = "Requested By")]
    public string RequestedByEmail { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Status")]
    public RequestStatusEnum Status { get; set; }
    
    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; }
}

public class AdminRequestCreateViewModel
{
    [Required]
    [Display(Name = "Request Type")]
    public AdminRequestTypeEnum RequestType { get; set; }
    
    [Required]
    [EmailAddress]
    [StringLength(256)]
    [Display(Name = "Target Email")]
    public string TargetEmail { get; set; } = string.Empty;
    
    [StringLength(200)]
    [Display(Name = "Full Name")]
    public string TargetFullName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(1000)]
    [Display(Name = "Justification")]
    public string Justification { get; set; } = string.Empty;
}

public class AdminRequestApproveViewModel
{
    public Guid Id { get; set; }
    public AdminRequestTypeEnum RequestType { get; set; }
    public string TargetEmail { get; set; } = string.Empty;
    public string TargetFullName { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public string RequestedByEmail { get; set; } = string.Empty;
}


