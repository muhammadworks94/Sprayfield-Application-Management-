using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Services.Models;

namespace SAM.ViewModels.Reports;

public class GroundwaterQualityReportsPageViewModel
{
    public Guid? SelectedCompanyId { get; set; }
    public SelectList? Facilities { get; set; }
    public SelectList? MonitoringWells { get; set; }
    public SelectList? Months { get; set; }
    public List<SelectListItem> Years { get; set; } = new();
    public GroundwaterQualityFilterViewModel Filter { get; set; } = new();
    public GroundwaterQualitySortViewModel Sort { get; set; } = new();
    public PagedResult<GroundwaterQualityReportRowViewModel> Reports { get; set; } = new();
}

public class GroundwaterQualityFilterViewModel
{
    public Guid? FacilityId { get; set; }
    public Guid? MonitoringWellId { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class GroundwaterQualitySortViewModel
{
    public string SortBy { get; set; } = "sampledate";
    public string SortDir { get; set; } = "desc";
}

public class GroundwaterQualityReportRowViewModel
{
    public Guid Id { get; set; }
    public Guid FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string MonitoringWellName { get; set; } = string.Empty;
    public DateTime SampleDate { get; set; }
    public string PermitDisplay { get; set; } = string.Empty;
    public string CollectedBy { get; set; } = string.Empty;
    public string AnalyzedBy { get; set; } = string.Empty;
    public DateTime? UpdatedDate { get; set; }
}

public sealed class Gw59ParameterSnapshot
{
    public int SortOrder { get; set; }
    public string PcsCode { get; set; } = string.Empty;
    public string ParameterName { get; set; } = string.Empty;
    public string Units { get; set; } = string.Empty;
    public decimal? Value { get; set; }
    public decimal? DailyMaximumLimit { get; set; }
    public bool IsGw59 { get; set; }
    public bool IsGw59A { get; set; }
}

public sealed class Gw59ExportModel
{
    public Guid GwMonitId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string PermitNumber { get; set; } = string.Empty;
    public string Permittee { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public string FacilityPhone { get; set; } = string.Empty;
    public DateTime? PermitExpirationDate { get; set; }
    public string WellId { get; set; } = string.Empty;
    public string WellLocation { get; set; } = string.Empty;
    public decimal? WellDepthFeet { get; set; }
    public decimal? DiameterInches { get; set; }
    public decimal? LowScreenDepthFeet { get; set; }
    public decimal? HighScreenDepthFeet { get; set; }
    public int? NumberOfWellsToBeSampled { get; set; }
    public DateTime SampleDate { get; set; }
    public decimal? SampleDepth { get; set; }
    public decimal? WaterLevel { get; set; }
    public decimal? GallonsPumped { get; set; }
    public decimal? PHField { get; set; }
    public decimal? TemperatureField { get; set; }
    public decimal? SpecificConductance { get; set; }
    public string Odor { get; set; } = string.Empty;
    public string Appearance { get; set; } = string.Empty;
    public bool MetalsUnfiltered { get; set; }
    public bool MetalsAcidified { get; set; }
    public decimal? TDS { get; set; }
    public decimal? TOC { get; set; }
    public decimal? Chloride { get; set; }
    public decimal? NH3N { get; set; }
    public decimal? NO3N { get; set; }
    public decimal? TKN { get; set; }
    public decimal? Calcium { get; set; }
    public decimal? Magnesium { get; set; }
    public decimal? FecalColiform { get; set; }
    public decimal? TotalColiform { get; set; }
    public string LabName { get; set; } = string.Empty;
    public string LabCertificationNumber { get; set; } = string.Empty;
    public string CollectedBy { get; set; } = string.Empty;
    public string AnalyzedBy { get; set; } = string.Empty;
    public bool LabReportAttached { get; set; }
    public string VOCMethodNumber { get; set; } = string.Empty;
    public string CertificationName { get; set; } = string.Empty;
    public string CertificationTitle { get; set; } = string.Empty;
    public DateTime? CertificationDate { get; set; }
    public string? VOCReportFileStoragePath { get; set; }
    public bool HasGw59APermitTemplateRows { get; set; }
    public List<Gw59ParameterSnapshot> ParameterSnapshots { get; set; } = new();

    public bool? GW59AQuestion1Response { get; set; }
    public bool? GW59AQuestion2Response { get; set; }
    public bool? GW59AQuestion3Response { get; set; }
    public bool? GW59AQuestion4Response { get; set; }
    public bool? GW59AQuestion5Response { get; set; }
    public bool? GW59AQuestion6Response { get; set; }
    public bool? GW59AQuestion7Response { get; set; }
    public DateTime? GW59ADueDate { get; set; }
    public string GW59AQuestion2Details { get; set; } = string.Empty;
    public string GW59AQuestion4Details { get; set; } = string.Empty;
    public string GW59AQuestion5Details { get; set; } = string.Empty;
    public string GW59AQuestion7Details { get; set; } = string.Empty;
    public string GW59ASignerName { get; set; } = string.Empty;
    public string GW59ASignerTitle { get; set; } = string.Empty;
    public DateTime? GW59ASignedDate { get; set; }
}
