using SAM.Domain.Entities;

namespace SAM.Services.Models;

public enum NdarRefreshStatus
{
    Updated = 1,
    NoReport = 2,
    Failed = 3,
    Created = 4
}

public sealed class NdarRefreshOutcome
{
    public Guid FacilityId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public NdarRefreshStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;

    public bool Created { get; set; }

    public bool Attempted => Status != NdarRefreshStatus.NoReport;
    public bool Updated => Status == NdarRefreshStatus.Updated;
    public bool SkippedNoReport => Status == NdarRefreshStatus.NoReport;
    public bool Failed => Status == NdarRefreshStatus.Failed;
    public bool WasCreated => Status == NdarRefreshStatus.Created || Created;

    public List<string> SecondaryWarnings { get; set; } = new();
}

public sealed class MonthlyApplicationMutationResult
{
    public MonthlyApplication Application { get; set; } = new();
    public List<NdarRefreshOutcome> NdarRefreshOutcomes { get; set; } = new();
}

public sealed class OperatorLogMutationResult
{
    public OperatorLog OperatorLog { get; set; } = new();
    public List<NdarRefreshOutcome> NdarRefreshOutcomes { get; set; } = new();
}
