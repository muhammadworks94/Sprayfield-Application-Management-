using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.CompanyManagement;

public class NewClientSetupBaselineViewModel
{
    public Guid CompanyId { get; set; }

    public string? CompanyName { get; set; }

    [Range(2000, 2100)]
    public int ThroughYear { get; set; }

    [Range(1, 12)]
    public int ThroughMonth { get; set; }

    public List<ClientSetupFacilityGroupViewModel> FacilityGroups { get; set; } = new();

    public int SavedCellCount { get; set; }
}

public class ClientSetupFacilityGroupViewModel
{
    public Guid FacilityId { get; set; }

    public string FacilityName { get; set; } = string.Empty;

    public List<ClientSetupSprayfieldColumnViewModel> SprayfieldColumns { get; set; } = new();

    public List<ClientSetupMonthRowViewModel> MonthRows { get; set; } = new();
}

public class ClientSetupCellSaveResponse
{
    public bool Success { get; set; }

    public string? Message { get; set; }
}

public class ToggleCompanyVerifiedRequest
{
    public Guid CompanyId { get; set; }

    public bool IsVerified { get; set; }
}
