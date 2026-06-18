namespace SAM.Services.Interfaces;

public enum PermitAlertSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}

public sealed class PermitAlert
{
    public Guid FacilityId { get; init; }
    public string FacilityName { get; init; } = string.Empty;
    public string PermitNumber { get; init; } = string.Empty;
    public string PermitVersion { get; init; } = string.Empty;
    public int DaysUntilExpiration { get; init; }
    public DateTime? EffectiveEndDate { get; init; }
    public PermitAlertSeverity Severity { get; init; }
    public string Message { get; init; } = string.Empty;
}

public interface IPermitAlertService
{
    Task<IReadOnlyList<PermitAlert>> GetAlertsAsync(Guid? companyId, DateTime asOfUtc);
}
