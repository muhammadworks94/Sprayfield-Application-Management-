using System.ComponentModel.DataAnnotations;
using SAM.Domain.Entities;

namespace SAM.ViewModels.SystemAdmin;

public class CompanyViewModel
{
    public Guid Id { get; set; }
    
    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters.")]
    [Display(Name = "Company Name")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Contact email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
    [Display(Name = "Contact Email")]
    public string ContactEmail { get; set; } = string.Empty;
    
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Display(Name = "Website")]
    public string? Website { get; set; }
    
    [Display(Name = "Description")]
    public string? Description { get; set; }
    
    [Display(Name = "Tax ID")]
    public string? TaxId { get; set; }
    
    [Display(Name = "Fax Number")]
    public string? FaxNumber { get; set; }
    
    [Display(Name = "License Number")]
    public string? LicenseNumber { get; set; }
    
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
    
    [Display(Name = "Verified")]
    public bool IsVerified { get; set; } = true;
    
    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; }
    
    [Display(Name = "Updated Date")]
    public DateTime? UpdatedDate { get; set; }
    
    [Display(Name = "Created By")]
    public string CreatedBy { get; set; } = string.Empty;
}

public class CompanyCreateViewModel
{
    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters.")]
    [Display(Name = "Company Name")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Contact email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
    [Display(Name = "Contact Email")]
    public string ContactEmail { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Invalid phone number format.")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Url(ErrorMessage = "Invalid website URL format.")]
    [StringLength(500, ErrorMessage = "Website URL cannot exceed 500 characters.")]
    [Display(Name = "Website")]
    public string? Website { get; set; }
    
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }
    
    [StringLength(50, ErrorMessage = "Tax ID cannot exceed 50 characters.")]
    [Display(Name = "Tax ID")]
    public string? TaxId { get; set; }
    
    [Phone(ErrorMessage = "Invalid fax number format.")]
    [StringLength(20, ErrorMessage = "Fax number cannot exceed 20 characters.")]
    [Display(Name = "Fax Number")]
    public string? FaxNumber { get; set; }
    
    [StringLength(100, ErrorMessage = "License number cannot exceed 100 characters.")]
    [Display(Name = "License Number")]
    public string? LicenseNumber { get; set; }
    
    [Display(Name = "Active Company")]
    public bool IsActive { get; set; } = true;
    
    [Display(Name = "Verified Company")]
    public bool IsVerified { get; set; } = true;
}

public class CompanyEditViewModel
{
    public Guid Id { get; set; }
    
    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters.")]
    [Display(Name = "Company Name")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Contact email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
    [Display(Name = "Contact Email")]
    public string ContactEmail { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Invalid phone number format.")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Url(ErrorMessage = "Invalid website URL format.")]
    [StringLength(500, ErrorMessage = "Website URL cannot exceed 500 characters.")]
    [Display(Name = "Website")]
    public string? Website { get; set; }
    
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }
    
    [StringLength(50, ErrorMessage = "Tax ID cannot exceed 50 characters.")]
    [Display(Name = "Tax ID")]
    public string? TaxId { get; set; }
    
    [Phone(ErrorMessage = "Invalid fax number format.")]
    [StringLength(20, ErrorMessage = "Fax number cannot exceed 20 characters.")]
    [Display(Name = "Fax Number")]
    public string? FaxNumber { get; set; }
    
    [StringLength(100, ErrorMessage = "License number cannot exceed 100 characters.")]
    [Display(Name = "License Number")]
    public string? LicenseNumber { get; set; }
    
    [Display(Name = "Active Company")]
    public bool IsActive { get; set; } = true;
    
    [Display(Name = "Verified Company")]
    public bool IsVerified { get; set; } = true;
}


