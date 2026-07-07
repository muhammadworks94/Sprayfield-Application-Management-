using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.CompanyManagement;

public class ClientSetupViewModel
{
    public Guid CompanyId { get; set; }

    public Guid FacilityId { get; set; }

    [Range(2000, 2100)]
    public int ThroughYear { get; set; }

    [Range(1, 12)]
    public int ThroughMonth { get; set; }

    public bool RefreshNdarAfterSave { get; set; }

    public string? FacilityName { get; set; }

    public string? CompanyName { get; set; }

    public List<ClientSetupSprayfieldColumnViewModel> SprayfieldColumns { get; set; } = new();

    public List<ClientSetupMonthRowViewModel> MonthRows { get; set; } = new();
}

public class ClientSetupSprayfieldColumnViewModel
{
    public Guid SprayfieldId { get; set; }
    public string FieldCode { get; set; } = string.Empty;
    public decimal? Acres { get; set; }
}

public class ClientSetupMonthRowViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = string.Empty;
    public List<ClientSetupCellViewModel> Cells { get; set; } = new();
}

public class ClientSetupCellViewModel
{
    public Guid SprayfieldId { get; set; }
    public decimal? LoadingInches { get; set; }
    public bool IsReadOnly { get; set; }
    public string Source { get; set; } = "Empty";
}

public class ClientSetupCellSaveRequest
{
    public Guid SprayfieldId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal? LoadingInches { get; set; }
}
