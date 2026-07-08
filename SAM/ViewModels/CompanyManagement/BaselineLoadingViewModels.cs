namespace SAM.ViewModels.CompanyManagement;

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
    public Guid FacilityId { get; set; }

    public Guid SprayfieldId { get; set; }

    public int ThroughYear { get; set; }

    public int ThroughMonth { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public decimal? LoadingInches { get; set; }
}
