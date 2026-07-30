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
    [Display(Name = "Permit Name (if different than facility)")]
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
    [Display(Name = "Facility Contact Person")]
    public string FacilityContactPerson { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Facility Contact Person Phone #")]
    public string FacilityContactPersonPhone { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Signing Official")]
    public string? SigningOfficial { get; set; }

    [StringLength(200)]
    [Display(Name = "Signing Official's Title")]
    public string? FacilityContactPersonTitle { get; set; }

    [StringLength(200)]
    [Display(Name = "Operator in Responsible Charge (ORC)")]
    public string OrcName { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Operator Grade")]
    public string OperatorGrade { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Operator Number")]
    public string OperatorNumber { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Operator Phone")]
    public string OperatorPhone { get; set; } = string.Empty;

    [Display(Name = "Current ORC Start")]
    public DateTime? CurrentOrcStartDate { get; set; }

    [Display(Name = "Assigned SAM User")]
    public string? CurrentOrcUserName { get; set; }

    public List<FacilityOrcAssignmentItemViewModel> OrcAssignmentHistory { get; set; } = new();

    [StringLength(200)]
    [Display(Name = "Person(s) Collecting Samples")]
    public string PersonsCollectingSamples { get; set; } = string.Empty;

    [Display(Name = "Mineralization Rate (%)")]
    [Range(0, 100)]
    public decimal? MineralizationRatePercent { get; set; }

    [Display(Name = "Volatilization Rate (%)")]
    [Range(0, 100)]
    public decimal? VolatilizationRatePercent { get; set; }

    [Display(Name = "Select Permit")]
    public Guid? DefaultFacilityPermitId { get; set; }

    public string? SelectedPermitLabel { get; set; }

    public string? PermitLocationSummary { get; set; }

    public string? SelectedPermitCounty { get; set; }

    public int? PermitSprayfieldCount { get; set; }

    public decimal? PermitMinimumFreeboardFeet { get; set; }

    [Display(Name = "Lagoon Berm Height (ft)")]
    public decimal? LagoonBermHeightFeet { get; set; }
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
    [StringLength(200)]
    [Display(Name = "Permit Name (if different than facility)")]
    public string Permittee { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Facility Class")]
    public string FacilityClass { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Permittee Phone")]
    public string? PermitPhone { get; set; }

    [StringLength(50)]
    [Display(Name = "Facility Phone")]
    public string? FacilityPhone { get; set; }

    [StringLength(200)]
    [Display(Name = "Facility Contact Person")]
    public string? FacilityContactPerson { get; set; }

    [StringLength(50)]
    [Display(Name = "Facility Contact Person Phone #")]
    public string? FacilityContactPersonPhone { get; set; }

    [StringLength(200)]
    [Display(Name = "Signing Official")]
    public string? SigningOfficial { get; set; }

    [StringLength(200)]
    [Display(Name = "Signing Official's Title")]
    public string? FacilityContactPersonTitle { get; set; }

    [StringLength(200)]
    [Display(Name = "Operator in Responsible Charge (ORC)")]
    public string? OrcName { get; set; }

    [StringLength(50)]
    [Display(Name = "Operator Grade")]
    public string? OperatorGrade { get; set; }

    [StringLength(50)]
    [Display(Name = "Operator Number")]
    public string? OperatorNumber { get; set; }

    [StringLength(50)]
    [Display(Name = "Operator Phone")]
    public string? OperatorPhone { get; set; }

    [Display(Name = "ORC SAM User")]
    public string? OrcUserId { get; set; }

    [Display(Name = "ORC Assignment Start Date")]
    [DataType(DataType.Date)]
    public DateTime? OrcAssignmentStartDate { get; set; }

    [StringLength(200)]
    [Display(Name = "Person(s) Collecting Samples")]
    public string? PersonsCollectingSamples { get; set; }

    [Display(Name = "Mineralization Rate (%)")]
    [Range(0, 100)]
    public decimal? MineralizationRatePercent { get; set; }

    [Display(Name = "Volatilization Rate (%)")]
    [Range(0, 100)]
    public decimal? VolatilizationRatePercent { get; set; }

    [Display(Name = "Lagoon Berm Height (ft)")]
    public decimal? LagoonBermHeightFeet { get; set; }
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
    [StringLength(200)]
    [Display(Name = "Permit Name (if different than facility)")]
    public string Permittee { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Facility Class")]
    public string FacilityClass { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Permittee Phone")]
    public string? PermitPhone { get; set; }

    [StringLength(50)]
    [Display(Name = "Facility Phone")]
    public string? FacilityPhone { get; set; }

    [StringLength(200)]
    [Display(Name = "Facility Contact Person")]
    public string? FacilityContactPerson { get; set; }

    [StringLength(50)]
    [Display(Name = "Facility Contact Person Phone #")]
    public string? FacilityContactPersonPhone { get; set; }

    [StringLength(200)]
    [Display(Name = "Signing Official")]
    public string? SigningOfficial { get; set; }

    [StringLength(200)]
    [Display(Name = "Signing Official's Title")]
    public string? FacilityContactPersonTitle { get; set; }

    [StringLength(200)]
    [Display(Name = "Operator in Responsible Charge (ORC)")]
    public string? OrcName { get; set; }

    [StringLength(50)]
    [Display(Name = "Operator Grade")]
    public string? OperatorGrade { get; set; }

    [StringLength(50)]
    [Display(Name = "Operator Number")]
    public string? OperatorNumber { get; set; }

    [StringLength(50)]
    [Display(Name = "Operator Phone")]
    public string? OperatorPhone { get; set; }

    [Display(Name = "ORC SAM User")]
    public string? OrcUserId { get; set; }

    [Display(Name = "Assign / Replace ORC Start Date")]
    [DataType(DataType.Date)]
    public DateTime? OrcAssignmentStartDate { get; set; }

    [Display(Name = "End Current ORC As Of")]
    [DataType(DataType.Date)]
    public DateTime? EndCurrentOrcAsOf { get; set; }

    public string? CurrentOrcUserId { get; set; }

    public DateTime? CurrentOrcStartDate { get; set; }

    public List<FacilityOrcAssignmentItemViewModel> OrcAssignmentHistory { get; set; } = new();

    [StringLength(200)]
    [Display(Name = "Person(s) Collecting Samples")]
    public string? PersonsCollectingSamples { get; set; }

    [Display(Name = "Mineralization Rate (%)")]
    [Range(0, 100)]
    public decimal? MineralizationRatePercent { get; set; }

    [Display(Name = "Volatilization Rate (%)")]
    [Range(0, 100)]
    public decimal? VolatilizationRatePercent { get; set; }

    [Display(Name = "Select Permit")]
    public Guid? DefaultFacilityPermitId { get; set; }

    [Display(Name = "Lagoon Berm Height (ft)")]
    public decimal? LagoonBermHeightFeet { get; set; }
}

public class FacilityOrcAssignmentItemViewModel
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? OperatorNumber { get; set; }
    public string? OperatorGrade { get; set; }
    public string? OperatorPhone { get; set; }
    public bool IsCurrent => EndDate == null;
}


