using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.UserManagement;

public class AdminViewModel
{
    public string Id { get; set; } = string.Empty;
    
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;
    
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    
    [Display(Name = "Company")]
    public string? CompanyName { get; set; }
    
    [Display(Name = "Status")]
    public bool IsActive { get; set; }
}

public class AdminCreateViewModel
{
    [Required]
    [StringLength(200)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}

public class AdminPromoteViewModel
{
    [Required]
    [Display(Name = "User")]
    public string UserId { get; set; } = string.Empty;
}

