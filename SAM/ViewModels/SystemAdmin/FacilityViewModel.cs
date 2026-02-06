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
    [Display(Name = "Permittee Street Address")]
    public string Address { get; set; } = string.Empty;
    
    [StringLength(100)]
    [Display(Name = "Permittee City")]
    public string City { get; set; } = string.Empty;
    
    [StringLength(50)]
    [Display(Name = "Permittee State")]
    public string State { get; set; } = string.Empty;
    
    [StringLength(20)]
    [Display(Name = "Zip Code")]
    public string ZipCode { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "County")]
    public string County { get; set; } = string.Empty;

    // Permit / facility details

    [Display(Name = "Permit Expiration Date")]
    [DataType(DataType.Date)]
    public DateTime? PermitExpirationDate { get; set; }

    [StringLength(50)]
    [Display(Name = "Permittee Phone")]
    public string PermitPhone { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Facility Phone")]
    public string FacilityPhone { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Operator in Responsible Charge (ORC)")]
    public string OrcName { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Operator Grade")]
    public string OperatorGrade { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Operator Number")]
    public string OperatorNumber { get; set; } = string.Empty;

    [Display(Name = "Change in ORC?")]
    public bool? ChangeInOrc { get; set; }

    [Display(Name = "Total Number of Sprayfields")]
    public int? TotalNumberOfSprayfields { get; set; }

    [StringLength(200)]
    [Display(Name = "Certified Laboratory (1)")]
    public string CertifiedLaboratory1Name { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Certified Laboratory (2)")]
    public string CertifiedLaboratory2Name { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Lab Certification No. (1)")]
    public string LabCertificationNumber1 { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Lab Certification No. (2)")]
    public string LabCertificationNumber2 { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Person(s) Collecting Samples")]
    public string PersonsCollectingSamples { get; set; } = string.Empty;

    [Display(Name = "Permitted Minimum Freeboard (ft)")]
    public decimal? PermittedMinimumFreeboardFeet { get; set; }
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
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Facility Class")]
    public string FacilityClass { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    [Display(Name = "Permittee Street Address")]
    public string Address { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Permittee City")]
    public string City { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    [Display(Name = "Permittee State")]
    public string State { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    [Display(Name = "Zip Code")]
    public string ZipCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "County")]
    public string County { get; set; } = string.Empty;

    [Display(Name = "Permit Expiration Date")]
    [DataType(DataType.Date)]
    public DateTime? PermitExpirationDate { get; set; }

    [StringLength(50)]
    [Display(Name = "Permittee Phone")]
    public string? PermitPhone { get; set; }

    [StringLength(50)]
    [Display(Name = "Facility Phone")]
    public string? FacilityPhone { get; set; }

    [StringLength(200)]
    [Display(Name = "Operator in Responsible Charge (ORC)")]
    public string? OrcName { get; set; }

    [StringLength(50)]
    [Display(Name = "Operator Grade")]
    public string? OperatorGrade { get; set; }

    [StringLength(50)]
    [Display(Name = "Operator Number")]
    public string? OperatorNumber { get; set; }

    [Display(Name = "Change in ORC?")]
    public bool ChangeInOrc { get; set; }

    [Display(Name = "Total Number of Sprayfields")]
    public int? TotalNumberOfSprayfields { get; set; }

    [StringLength(200)]
    [Display(Name = "Certified Laboratory (1)")]
    public string? CertifiedLaboratory1Name { get; set; }

    [StringLength(200)]
    [Display(Name = "Certified Laboratory (2)")]
    public string? CertifiedLaboratory2Name { get; set; }

    [StringLength(100)]
    [Display(Name = "Lab Certification No. (1)")]
    public string? LabCertificationNumber1 { get; set; }

    [StringLength(100)]
    [Display(Name = "Lab Certification No. (2)")]
    public string? LabCertificationNumber2 { get; set; }

    [StringLength(200)]
    [Display(Name = "Person(s) Collecting Samples")]
    public string? PersonsCollectingSamples { get; set; }

    [Display(Name = "Permitted Minimum Freeboard (ft)")]
    public decimal? PermittedMinimumFreeboardFeet { get; set; }
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
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Facility Class")]
    public string FacilityClass { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    [Display(Name = "Permittee StreetAddress")]
    public string Address { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Permittee City")]
    public string City { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    [Display(Name = "Permittee State")]
    public string State { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    [Display(Name = "Zip Code")]
    public string ZipCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "County")]
    public string County { get; set; } = string.Empty;

    [Display(Name = "Permit Expiration Date")]
    [DataType(DataType.Date)]
    public DateTime? PermitExpirationDate { get; set; }

    [StringLength(50)]
    [Display(Name = "Permittee Phone")]
    public string? PermitPhone { get; set; }

    [StringLength(50)]
    [Display(Name = "Facility Phone")]
    public string? FacilityPhone { get; set; }

    [StringLength(200)]
    [Display(Name = "Operator in Responsible Charge (ORC)")]
    public string? OrcName { get; set; }

    [StringLength(50)]
    [Display(Name = "Operator Grade")]
    public string? OperatorGrade { get; set; }

    [StringLength(50)]
    [Display(Name = "Operator Number")]
    public string? OperatorNumber { get; set; }

    [Display(Name = "Change in ORC?")]
    public bool? ChangeInOrc { get; set; }

    [Display(Name = "Total Number of Sprayfields")]
    public int? TotalNumberOfSprayfields { get; set; }

    [StringLength(200)]
    [Display(Name = "Certified Laboratory (1)")]
    public string? CertifiedLaboratory1Name { get; set; }

    [StringLength(200)]
    [Display(Name = "Certified Laboratory (2)")]
    public string? CertifiedLaboratory2Name { get; set; }

    [StringLength(100)]
    [Display(Name = "Lab Certification No. (1)")]
    public string? LabCertificationNumber1 { get; set; }

    [StringLength(100)]
    [Display(Name = "Lab Certification No. (2)")]
    public string? LabCertificationNumber2 { get; set; }

    [StringLength(200)]
    [Display(Name = "Person(s) Collecting Samples")]
    public string? PersonsCollectingSamples { get; set; }

    [Display(Name = "Permitted Minimum Freeboard (ft)")]
    public decimal? PermittedMinimumFreeboardFeet { get; set; }
}


