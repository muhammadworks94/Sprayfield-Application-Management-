using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.OperationalData;

/// <summary>
/// Read-only view model representing a single GW-59 groundwater quality monitoring report.
/// </summary>
public class GW59ReportViewModel
{
    // Facility information
    public Guid FacilityId { get; set; }

    [Display(Name = "Facility Name")]
    public string FacilityName { get; set; } = string.Empty;

    [Display(Name = "Permit Number")]
    public string PermitNumber { get; set; } = string.Empty;

    [Display(Name = "Permittee")]
    public string Permittee { get; set; } = string.Empty;

    [Display(Name = "Facility Address")]
    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;

    [Display(Name = "Facility Phone")]
    public string FacilityPhone { get; set; } = string.Empty;

    [Display(Name = "Permit Expiration Date")]
    [DataType(DataType.Date)]
    public DateTime? PermitExpirationDate { get; set; }

    // Well / site information
    public Guid MonitoringWellId { get; set; }

    [Display(Name = "Well ID")]
    public string WellId { get; set; } = string.Empty;

    [Display(Name = "Well Location / Site Name")]
    public string WellLocation { get; set; } = string.Empty;

    [Display(Name = "Well Depth (ft)")]
    public decimal? WellDepthFeet { get; set; }

    [Display(Name = "Well Diameter (in)")]
    public decimal? DiameterInches { get; set; }

    [Display(Name = "Screened Interval Low (ft)")]
    public decimal? LowScreenDepthFeet { get; set; }

    [Display(Name = "Screened Interval High (ft)")]
    public decimal? HighScreenDepthFeet { get; set; }

    [Display(Name = "Number of Wells to be Sampled")]
    public int? NumberOfWellsToBeSampled { get; set; }

    // Sampling information / field analyses

    [Display(Name = "Sample Date")]
    [DataType(DataType.Date)]
    public DateTime SampleDate { get; set; }

    [Display(Name = "Sample Depth (ft)")]
    public decimal? SampleDepth { get; set; }

    [Display(Name = "Depth to Water Level (ft)")]
    public decimal? WaterLevel { get; set; }

    [Display(Name = "Gallons Pumped")]
    public decimal? GallonsPumped { get; set; }

    [Display(Name = "Field pH")]
    public decimal? PHField { get; set; }

    [Display(Name = "Field Temperature (°C)")]
    public decimal? TemperatureField { get; set; }

    [Display(Name = "Specific Conductance (uMhos)")]
    public decimal? SpecificConductance { get; set; }

    [Display(Name = "Odor")]
    public string Odor { get; set; } = string.Empty;

    [Display(Name = "Appearance")]
    public string Appearance { get; set; } = string.Empty;

    [Display(Name = "Metals Samples Collected Unfiltered")]
    public bool MetalsUnfiltered { get; set; }

    [Display(Name = "Metal Samples Field Acidified")]
    public bool MetalsAcidified { get; set; }

    // Laboratory parameters (subset supported by current entity)

    [Display(Name = "Total Dissolved Solids (mg/L)")]
    public decimal? TDS { get; set; }

    [Display(Name = "TOC (mg/L)")]
    public decimal? TOC { get; set; }

    [Display(Name = "Chloride (mg/L)")]
    public decimal? Chloride { get; set; }

    [Display(Name = "NH3-N (mg/L)")]
    public decimal? NH3N { get; set; }

    [Display(Name = "NO3-N (mg/L)")]
    public decimal? NO3N { get; set; }

    [Display(Name = "TKN (mg/L)")]
    public decimal? TKN { get; set; }

    [Display(Name = "Calcium (mg/L)")]
    public decimal? Calcium { get; set; }

    [Display(Name = "Magnesium (mg/L)")]
    public decimal? Magnesium { get; set; }

    [Display(Name = "Fecal Coliform (CFU/100mL)")]
    public decimal? FecalColiform { get; set; }

    [Display(Name = "Total Coliform (CFU/100mL)")]
    public decimal? TotalColiform { get; set; }

    // Laboratory identity

    [Display(Name = "Laboratory Name")]
    public string LabName { get; set; } = string.Empty;

    [Display(Name = "Lab Certification No.")]
    public string LabCertificationNumber { get; set; } = string.Empty;

    [Display(Name = "Lab Report Attached")]
    public bool LabReportAttached { get; set; }

    [Display(Name = "VOC Method #")]
    public string VOCMethodNumber { get; set; } = string.Empty;

    // Certification block (signature area)

    [Display(Name = "Certification Name")]
    public string CertificationName { get; set; } = string.Empty;

    [Display(Name = "Certification Title")]
    public string CertificationTitle { get; set; } = string.Empty;

    [Display(Name = "Certification Date")]
    [DataType(DataType.Date)]
    public DateTime? CertificationDate { get; set; }
}

