using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

/// <summary>
/// Persisted derived load values for auditing and fast reporting.
/// </summary>
public class LoadCalculation : AuditableEntity
{
    public Guid ApplicationId { get; set; }
    public decimal LbsApplied { get; set; }
    public decimal LbsPerAcre { get; set; }
    public decimal ZoneAcresSnapshot { get; set; }
    public decimal ZonePercentSnapshot { get; set; }
    public string FormulaVersion { get; set; } = "v1";
    public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;

    public MonthlyApplication? Application { get; set; }
}
