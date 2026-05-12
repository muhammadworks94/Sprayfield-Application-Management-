using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Services.Models;

namespace SAM.ViewModels.OperationalData;

public class MonthlyApplicationViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public Guid FacilityId { get; set; }
    public string? FacilityName { get; set; }
    public Guid SprayfieldId { get; set; }
    public string? ZoneName { get; set; }
    public string? SprayfieldName { get; set; }
    public decimal? ZoneAcres { get; set; }
    public DateTime ApplicationDate { get; set; }
    public decimal? DailyLoadingInches { get; set; }
    public decimal VolumeGallons { get; set; }
    public decimal? TimeIrrigatedMinutes { get; set; }
    public decimal MaximumHourlyLoadingInchesPerAcre { get; set; }
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
    [Display(Name = "Application Date")]
    [DataType(DataType.Date)]
    public DateTime ApplicationDate { get; set; } = DateTime.Today;

    [Display(Name = "Daily Loading (inches)")]
    public decimal? DailyLoadingInches { get; set; }

    [Display(Name = "Volume Applied (gallons)")]
    public decimal? VolumeGallons { get; set; }

    [Range(0.0, double.MaxValue)]
    [Display(Name = "Time Irrigated (minutes)")]
    public decimal? TimeIrrigatedMinutes { get; set; }

    [Display(Name = "Maximum Hourly Loading (inches/acre)")]
    [DisplayFormat(DataFormatString = "{0:0.00}", ApplyFormatInEditMode = true)]
    public decimal? MaximumHourlyLoadingInchesPerAcre { get; set; }

    [Display(Name = "Comments")]
    public string? Comments { get; set; }

    public bool ConfirmComplianceWarnings { get; set; }
    public string? ComplianceWarningSummary { get; set; }
}

public class MonthlyApplicationEditViewModel : MonthlyApplicationCreateViewModel
{
    public Guid Id { get; set; }
}

public class MonthlyApplicationFilterViewModel
{
    public Guid? FacilityId { get; set; }
    public Guid? SprayfieldId { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class MonthlyApplicationSortViewModel
{
    public string SortBy { get; set; } = "applicationDate";
    public string SortDir { get; set; } = "desc";
}

public class MonthlyApplicationsIndexViewModel
{
    public SelectList? Facilities { get; set; }
    public SelectList? Sprayfields { get; set; }
    public MonthlyApplicationFilterViewModel Filter { get; set; } = new();
    public MonthlyApplicationSortViewModel Sort { get; set; } = new();
    public PagedResult<MonthlyApplicationViewModel> Applications { get; set; } = new();
}
