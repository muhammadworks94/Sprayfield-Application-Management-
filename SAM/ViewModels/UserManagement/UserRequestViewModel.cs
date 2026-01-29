using System.ComponentModel.DataAnnotations;
using SAM.Domain.Enums;

namespace SAM.ViewModels.UserManagement;

public class UserRequestViewModel
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Role")]
    public AppRoleEnum AppRole { get; set; }
    
    [Display(Name = "Requested By")]
    public string RequestedByEmail { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Status")]
    public RequestStatusEnum Status { get; set; }
    
    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; }
}

public class UserRequestCreateViewModel
{
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Role")]
    public AppRoleEnum AppRole { get; set; } = AppRoleEnum.@operator;
}

public class UserRequestApproveViewModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Role")]
    public AppRoleEnum AppRole { get; set; }
    
    public AppRoleEnum RequestedRole { get; set; }
    public string RequestedByEmail { get; set; } = string.Empty;
}


