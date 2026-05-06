using System.ComponentModel.DataAnnotations;
using SAM.Domain.Enums;

namespace SAM.ViewModels.Reports;

public class NDAR1ViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public Guid FacilityId { get; set; }
    public string? FacilityName { get; set; }
    
    [Required]
    [Display(Name = "Month")]
    public MonthEnum Month { get; set; }
    
    [Required]
    [Display(Name = "Year")]
    public int Year { get; set; }
    
    [Display(Name = "Did Irrigation Occur")]
    public bool DidIrrigationOccur { get; set; }
    
    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; }
    
    [Display(Name = "Updated Date")]
    public DateTime? UpdatedDate { get; set; }
}

public class NDAR1CreateViewModel
{
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [Display(Name = "Facility")]
    public Guid FacilityId { get; set; }
    
    [Required]
    [Display(Name = "Month")]
    public MonthEnum Month { get; set; } = (MonthEnum)DateTime.Now.Month;
    
    [Required]
    [Display(Name = "Year")]
    [Range(2000, 2100)]
    public int Year { get; set; } = DateTime.Now.Year;
}

public class NDAR1EditViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [Display(Name = "Facility")]
    public Guid FacilityId { get; set; }
    
    [Required]
    [Display(Name = "Month")]
    public MonthEnum Month { get; set; }
    
    [Required]
    [Display(Name = "Year")]
    [Range(2000, 2100)]
    public int Year { get; set; }
    
    [Display(Name = "Did Irrigation Occur")]
    public bool DidIrrigationOccur { get; set; }
    
    // Weather daily arrays
    [Display(Name = "Weather Code Daily")]
    public List<string?> WeatherCodeDaily { get; set; } = new List<string?>();
    
    [Display(Name = "Temperature Daily (°F)")]
    public List<decimal?> TemperatureDaily { get; set; } = new List<decimal?>();
    
    [Display(Name = "Precipitation Daily (in)")]
    public List<decimal?> PrecipitationDaily { get; set; } = new List<decimal?>();
    
    [Display(Name = "5-Day Upset Daily (ft)")]
    public List<decimal?> FiveDayUpsetDaily { get; set; } = new List<decimal?>();
    
    // Field 1
    [Display(Name = "Field 1")]
    public Guid? Field1Id { get; set; }
    
    [Display(Name = "Field 1 Volume Applied Daily (gal)")]
    public List<decimal?> Field1VolumeAppliedDaily { get; set; } = new List<decimal?>();
    
    [Display(Name = "Field 1 Time Irrigated Daily (min)")]
    public List<decimal?> Field1TimeIrrigatedDaily { get; set; } = new List<decimal?>();
    
    [Display(Name = "Field 1 Daily Loading (in)")]
    public List<decimal?> Field1DailyLoadingDaily { get; set; } = new List<decimal?>();
    
    [Display(Name = "Field 1 Max Hourly Loading (in)")]
    public List<decimal?> Field1MaxHourlyLoadingDaily { get; set; } = new List<decimal?>();
    
    [Display(Name = "Field 1 Monthly Loading (in)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal Field1MonthlyLoading { get; set; }
    
    [Display(Name = "Field 1 Max Hourly Loading (in)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal Field1MaxHourlyLoading { get; set; }
    
    [Display(Name = "Field 1 12-Month Floating Total (in)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal Field1TwelveMonthFloatingTotal { get; set; }
    
    // Field 2
    [Display(Name = "Field 2")]
    public Guid? Field2Id { get; set; }
    
    [Display(Name = "Field 2 Volume Applied Daily (gal)")]
    public List<decimal?> Field2VolumeAppliedDaily { get; set; } = new List<decimal?>();
    
    [Display(Name = "Field 2 Time Irrigated Daily (min)")]
    public List<decimal?> Field2TimeIrrigatedDaily { get; set; } = new List<decimal?>();
    
    [Display(Name = "Field 2 Daily Loading (in)")]
    public List<decimal?> Field2DailyLoadingDaily { get; set; } = new List<decimal?>();
    
    [Display(Name = "Field 2 Max Hourly Loading (in)")]
    public List<decimal?> Field2MaxHourlyLoadingDaily { get; set; } = new List<decimal?>();
    
    [Display(Name = "Field 2 Monthly Loading (in)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal Field2MonthlyLoading { get; set; }
    
    [Display(Name = "Field 2 Max Hourly Loading (in)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal Field2MaxHourlyLoading { get; set; }
    
    [Display(Name = "Field 2 12-Month Floating Total (in)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal Field2TwelveMonthFloatingTotal { get; set; }
    
    // Field 3
    [Display(Name = "Field 3")]
    public Guid? Field3Id { get; set; }
    
    [Display(Name = "Field 3 Volume Applied Daily (gal)")]
    public List<decimal?> Field3VolumeAppliedDaily { get; set; } = new List<decimal?>();
    
    [Display(Name = "Field 3 Time Irrigated Daily (min)")]
    public List<decimal?> Field3TimeIrrigatedDaily { get; set; } = new List<decimal?>();
    
    [Display(Name = "Field 3 Daily Loading (in)")]
    public List<decimal?> Field3DailyLoadingDaily { get; set; } = new List<decimal?>();
    
    [Display(Name = "Field 3 Max Hourly Loading (in)")]
    public List<decimal?> Field3MaxHourlyLoadingDaily { get; set; } = new List<decimal?>();
    
    [Display(Name = "Field 3 Monthly Loading (in)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal Field3MonthlyLoading { get; set; }
    
    [Display(Name = "Field 3 Max Hourly Loading (in)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal Field3MaxHourlyLoading { get; set; }
    
    [Display(Name = "Field 3 12-Month Floating Total (in)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal Field3TwelveMonthFloatingTotal { get; set; }
    
    // Field 4
    [Display(Name = "Field 4")]
    public Guid? Field4Id { get; set; }
    
    [Display(Name = "Field 4 Volume Applied Daily (gal)")]
    public List<decimal?> Field4VolumeAppliedDaily { get; set; } = new List<decimal?>();
    
    [Display(Name = "Field 4 Time Irrigated Daily (min)")]
    public List<decimal?> Field4TimeIrrigatedDaily { get; set; } = new List<decimal?>();
    
    [Display(Name = "Field 4 Daily Loading (in)")]
    public List<decimal?> Field4DailyLoadingDaily { get; set; } = new List<decimal?>();
    
    [Display(Name = "Field 4 Max Hourly Loading (in)")]
    public List<decimal?> Field4MaxHourlyLoadingDaily { get; set; } = new List<decimal?>();
    
    [Display(Name = "Field 4 Monthly Loading (in)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal Field4MonthlyLoading { get; set; }
    
    [Display(Name = "Field 4 Max Hourly Loading (in)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal Field4MaxHourlyLoading { get; set; }
    
    [Display(Name = "Field 4 12-Month Floating Total (in)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal Field4TwelveMonthFloatingTotal { get; set; }

    public List<NDAR1FieldEditViewModel> Fields { get; set; } = new();
}

public class NDAR1FieldEditViewModel
{
    public Guid? Id { get; set; }
    public Guid SprayfieldId { get; set; }
    public string FieldCode { get; set; } = string.Empty;
    public decimal? Acres { get; set; }
    public string CropSummary { get; set; } = string.Empty;
    public decimal MonthlyLoading { get; set; }
    public decimal MaxHourlyLoading { get; set; }
    public decimal TwelveMonthFloatingTotal { get; set; }
    public List<NDAR1FieldDailyEditViewModel> DailyValues { get; set; } = new();
}

public class NDAR1FieldDailyEditViewModel
{
    public int DayNo { get; set; }
    public decimal? VolumeApplied { get; set; }
    public decimal? TimeIrrigated { get; set; }
    public decimal? DailyLoading { get; set; }
    public decimal? MaxHourlyLoading { get; set; }
}

public class NDAR1EditGridViewModel
{
    public Guid NDAR1Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public MonthEnum Month { get; set; }
    public int Year { get; set; }
    public List<NDAR1GridFieldColumnViewModel> FieldColumns { get; set; } = new();
    public List<NDAR1GridDayRowViewModel> Rows { get; set; } = new();
}

public class NDAR1GridFieldColumnViewModel
{
    public Guid SprayfieldId { get; set; }
    public string FieldCode { get; set; } = string.Empty;
    public decimal? Acres { get; set; }
    public decimal MonthlyVolumeTotalGallons { get; set; }
    public decimal MonthlyDailyLoadingTotalInches { get; set; }
    public decimal TwelveMonthFloatingTotalInches { get; set; }
}

public class NDAR1GridDayRowViewModel
{
    public int DayNo { get; set; }
    public DateTime Date { get; set; }
    public string? WeatherCode { get; set; }
    public decimal? TemperatureF { get; set; }
    public decimal? PrecipitationIn { get; set; }
    public decimal? StorageFt { get; set; }
    public decimal? FiveDayUpsetFt { get; set; }
    public Guid? LockToken { get; set; }
    public string? LockedBy { get; set; }
    public bool IsLockedByCurrentUser { get; set; }
    public List<NDAR1GridApplicationCellViewModel> Applications { get; set; } = new();
}

public class NDAR1GridApplicationCellViewModel
{
    public Guid SprayfieldId { get; set; }
    public decimal? VolumeGallons { get; set; }
    public decimal? TimeIrrigatedMinutes { get; set; }
    public decimal? MaximumHourlyLoadingInchesPerAcre { get; set; }
    public decimal? DailyLoadingInches { get; set; }
}

public class NDAR1DayRowUpdateRequest
{
    public int DayNo { get; set; }
    public Guid LockToken { get; set; }
    public string? WeatherCode { get; set; }
    public decimal? TemperatureF { get; set; }
    public decimal? PrecipitationIn { get; set; }
    public decimal? StorageFt { get; set; }
    public decimal? FiveDayUpsetFt { get; set; }
    public List<NDAR1GridApplicationCellViewModel> Applications { get; set; } = new();
}

public class NDAR1RowEditLockResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid? LockToken { get; set; }
    public int DayNo { get; set; }
    public string? LockedBy { get; set; }
    public int? RemainingSeconds { get; set; }
}

public class NDAR1RowEditResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public bool IsValidationError { get; set; }
    public NDAR1GridDayRowViewModel? Row { get; set; }
}

