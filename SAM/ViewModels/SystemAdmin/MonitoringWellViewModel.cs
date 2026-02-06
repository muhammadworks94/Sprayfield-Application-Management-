using System.ComponentModel.DataAnnotations;
using SAM.Domain.Enums;

namespace SAM.ViewModels.SystemAdmin;

public class MonitoringWellViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Well Identifier #")]
    public string WellId { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Well Permit #")]
    public string? WellPermitNumber { get; set; }

    [StringLength(500)]
    [Display(Name = "Well Location")]
    public string LocationDescription { get; set; } = string.Empty;

    [Display(Name = "Diameter (inches)")]
    [Range(0, double.MaxValue, ErrorMessage = "Diameter must be a positive number.")]
    public decimal? DiameterInches { get; set; }

    [Display(Name = "Well Depth (feet)")]
    [Range(0, double.MaxValue, ErrorMessage = "Well depth must be a positive number.")]
    public decimal? WellDepthFeet { get; set; }

    [Display(Name = "Depth to Screen (feet)")]
    [Range(0, double.MaxValue, ErrorMessage = "Depth to screen must be a positive number.")]
    public decimal? DepthToScreenFeet { get; set; }

    [Display(Name = "Low Screen Depth (feet)")]
    [Range(0, double.MaxValue, ErrorMessage = "Low screen depth must be a positive number.")]
    public decimal? LowScreenDepthFeet { get; set; }

    [Display(Name = "High Screen Depth (feet)")]
    [Range(0, double.MaxValue, ErrorMessage = "High screen depth must be a positive number.")]
    public decimal? HighScreenDepthFeet { get; set; }

    [Display(Name = "Top of Casing Elevation (msl)")]
    public decimal? TopOfCasingElevationMsl { get; set; }

    [Display(Name = "Location of Well in regards to Treatment System")]
    public TreatmentSystemLocationEnum? TreatmentSystemLocation { get; set; }

    [Display(Name = "No. of Wells to be Sampled")]
    [Range(0, int.MaxValue, ErrorMessage = "Must be 0 or greater.")]
    public int? NumberOfWellsToBeSampled { get; set; }

    [Display(Name = "Latitude")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal? Latitude { get; set; }

    [Display(Name = "Longitude")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal? Longitude { get; set; }
}

public class MonitoringWellCreateViewModel
{
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Well Identifier #")]
    public string WellId { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Well Permit #")]
    public string? WellPermitNumber { get; set; }

    [StringLength(500)]
    [Display(Name = "Well Location")]
    public string LocationDescription { get; set; } = string.Empty;

    [Display(Name = "Diameter (inches)")]
    [Range(0, double.MaxValue, ErrorMessage = "Diameter must be a positive number.")]
    public decimal? DiameterInches { get; set; }

    [Display(Name = "Well Depth (feet)")]
    [Range(0, double.MaxValue, ErrorMessage = "Well depth must be a positive number.")]
    public decimal? WellDepthFeet { get; set; }

    [Display(Name = "Depth to Screen (feet)")]
    [Range(0, double.MaxValue, ErrorMessage = "Depth to screen must be a positive number.")]
    public decimal? DepthToScreenFeet { get; set; }

    [Display(Name = "Low Screen Depth (feet)")]
    [Range(0, double.MaxValue, ErrorMessage = "Low screen depth must be a positive number.")]
    public decimal? LowScreenDepthFeet { get; set; }

    [Display(Name = "High Screen Depth (feet)")]
    [Range(0, double.MaxValue, ErrorMessage = "High screen depth must be a positive number.")]
    public decimal? HighScreenDepthFeet { get; set; }

    [Display(Name = "Top of Casing Elevation (msl)")]
    public decimal? TopOfCasingElevationMsl { get; set; }

    [Display(Name = "Location of Well in regards to Treatment System")]
    public TreatmentSystemLocationEnum? TreatmentSystemLocation { get; set; }

    [Display(Name = "No. of Wells to be Sampled")]
    [Range(0, int.MaxValue, ErrorMessage = "Must be 0 or greater.")]
    public int? NumberOfWellsToBeSampled { get; set; }

    [Display(Name = "Latitude")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal? Latitude { get; set; }

    [Display(Name = "Longitude")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal? Longitude { get; set; }
}

public class MonitoringWellEditViewModel
{
    public Guid Id { get; set; }

    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Well Identifier #")]
    public string WellId { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Well Permit #")]
    public string? WellPermitNumber { get; set; }

    [StringLength(500)]
    [Display(Name = "Well Location")]
    public string LocationDescription { get; set; } = string.Empty;

    [Display(Name = "Diameter (inches)")]
    [Range(0, double.MaxValue, ErrorMessage = "Diameter must be a positive number.")]
    public decimal? DiameterInches { get; set; }

    [Display(Name = "Well Depth (feet)")]
    [Range(0, double.MaxValue, ErrorMessage = "Well depth must be a positive number.")]
    public decimal? WellDepthFeet { get; set; }

    [Display(Name = "Depth to Screen (feet)")]
    [Range(0, double.MaxValue, ErrorMessage = "Depth to screen must be a positive number.")]
    public decimal? DepthToScreenFeet { get; set; }

    [Display(Name = "Low Screen Depth (feet)")]
    [Range(0, double.MaxValue, ErrorMessage = "Low screen depth must be a positive number.")]
    public decimal? LowScreenDepthFeet { get; set; }

    [Display(Name = "High Screen Depth (feet)")]
    [Range(0, double.MaxValue, ErrorMessage = "High screen depth must be a positive number.")]
    public decimal? HighScreenDepthFeet { get; set; }

    [Display(Name = "Top of Casing Elevation (msl)")]
    public decimal? TopOfCasingElevationMsl { get; set; }

    [Display(Name = "Location of Well in regards to Treatment System")]
    public TreatmentSystemLocationEnum? TreatmentSystemLocation { get; set; }

    [Display(Name = "No. of Wells to be Sampled")]
    [Range(0, int.MaxValue, ErrorMessage = "Must be 0 or greater.")]
    public int? NumberOfWellsToBeSampled { get; set; }

    [Display(Name = "Latitude")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal? Latitude { get; set; }

    [Display(Name = "Longitude")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal? Longitude { get; set; }
}


