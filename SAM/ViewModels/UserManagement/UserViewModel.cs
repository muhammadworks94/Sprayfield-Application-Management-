using System.ComponentModel.DataAnnotations;
using SAM.Domain.Enums;

namespace SAM.ViewModels.UserManagement;

public class UserViewModel
{
    public string Id { get; set; } = string.Empty;
    
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;
    
    public Guid? CompanyId { get; set; }
    
    [Display(Name = "Company")]
    public string? CompanyName { get; set; }
    
    [Display(Name = "Roles")]
    public List<string> Roles { get; set; } = new List<string>();
    
    [Display(Name = "Active")]
    public bool IsActive { get; set; }
    
    [Display(Name = "Created Date")]
    public DateTime? CreatedDate { get; set; }
}

public class UserCreateViewModel
{
    [Required]
    [Display(Name = "Company")]
    public Guid? CompanyId { get; set; }
    
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

public class UserEditViewModel
{
    public string Id { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Company")]
    public Guid? CompanyId { get; set; }
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    
    [Display(Name = "Role")]
    public AppRoleEnum? AppRole { get; set; }
    
    [Display(Name = "Active")]
    public bool IsActive { get; set; }
}

public class UserDetailsViewModel : UserViewModel
{
    [Display(Name = "User Name")]
    public string UserName { get; set; } = string.Empty;
    
    [Display(Name = "Email Confirmed")]
    public bool EmailConfirmed { get; set; }
    
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }
    
    [Display(Name = "Updated Date")]
    public DateTime? UpdatedDate { get; set; }
}

