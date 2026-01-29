using System.ComponentModel.DataAnnotations;
using SAM.Domain.Entities;

namespace SAM.ViewModels.SystemAdmin;

public class FacilityViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    
    [Display(Name = "Company")]
    public string? CompanyName { get; set; }
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Facility Name")]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Permit Number")]
    public string PermitNumber { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Permittee")]
    public string Permittee { get; set; } = string.Empty;
    
    [StringLength(100)]
    [Display(Name = "Facility Class")]
    public string FacilityClass { get; set; } = string.Empty;
    
    [StringLength(500)]
    [Display(Name = "Address")]
    public string Address { get; set; } = string.Empty;
    
    [StringLength(100)]
    [Display(Name = "City")]
    public string City { get; set; } = string.Empty;
    
    [StringLength(50)]
    [Display(Name = "State")]
    public string State { get; set; } = string.Empty;
    
    [StringLength(20)]
    [Display(Name = "Zip Code")]
    public string ZipCode { get; set; } = string.Empty;
}

public class FacilityCreateViewModel
{
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Facility Name")]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Permit Number")]
    public string PermitNumber { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Permittee")]
    public string Permittee { get; set; } = string.Empty;
    
    [StringLength(100)]
    [Display(Name = "Facility Class")]
    public string FacilityClass { get; set; } = string.Empty;
    
    [StringLength(500)]
    [Display(Name = "Address")]
    public string Address { get; set; } = string.Empty;
    
    [StringLength(100)]
    [Display(Name = "City")]
    public string City { get; set; } = string.Empty;
    
    [StringLength(50)]
    [Display(Name = "State")]
    public string State { get; set; } = string.Empty;
    
    [StringLength(20)]
    [Display(Name = "Zip Code")]
    public string ZipCode { get; set; } = string.Empty;
}

public class FacilityEditViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Facility Name")]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Permit Number")]
    public string PermitNumber { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Permittee")]
    public string Permittee { get; set; } = string.Empty;
    
    [StringLength(100)]
    [Display(Name = "Facility Class")]
    public string FacilityClass { get; set; } = string.Empty;
    
    [StringLength(500)]
    [Display(Name = "Address")]
    public string Address { get; set; } = string.Empty;
    
    [StringLength(100)]
    [Display(Name = "City")]
    public string City { get; set; } = string.Empty;
    
    [StringLength(50)]
    [Display(Name = "State")]
    public string State { get; set; } = string.Empty;
    
    [StringLength(20)]
    [Display(Name = "Zip Code")]
    public string ZipCode { get; set; } = string.Empty;
}


