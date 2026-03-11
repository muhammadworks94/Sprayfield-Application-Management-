using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.OperationalData;

public class MonthlyApplicationViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public Guid FacilityId { get; set; }
    public string? FacilityName { get; set; }
    public Guid ZoneId { get; set; }
    public string? ZoneName { get; set; }
    public string? SprayfieldName { get; set; }
    public decimal? ZoneAcres { get; set; }
    public DateTime ApplicationDate { get; set; }
    public decimal VolumeGallons { get; set; }
    public decimal NitrogenMgL { get; set; }
    public string? OperatorSnapshotName { get; set; }
    public string? Comments { get; set; }
}

public class MonthlyApplicationCreateViewModel
{
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }

    [Required]
    [Display(Name = "Facility")]
    public Guid FacilityId { get; set; }

    [Required]
    [Display(Name = "Sprayfield")]
    public Guid SprayfieldId { get; set; }

    [Required]
    [Display(Name = "Application Zone")]
    public Guid ZoneId { get; set; }

    [Required]
    [Display(Name = "Application Date")]
    [DataType(DataType.Date)]
    public DateTime ApplicationDate { get; set; } = DateTime.Today;

    [Range(0.01, double.MaxValue)]
    [Display(Name = "Volume (gallons)")]
    public decimal VolumeGallons { get; set; }

    [Range(0.0, double.MaxValue)]
    [Display(Name = "Nitrogen (mg/L)")]
    public decimal NitrogenMgL { get; set; }

    [Display(Name = "Comments")]
    public string? Comments { get; set; }

    public bool ConfirmComplianceWarnings { get; set; }
    public string? ComplianceWarningSummary { get; set; }
}

public class MonthlyApplicationEditViewModel : MonthlyApplicationCreateViewModel
{
    public Guid Id { get; set; }
}
