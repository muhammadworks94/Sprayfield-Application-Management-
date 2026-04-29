using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

/// <summary>
/// Operational application log at zone level.
/// </summary>
public class MonthlyApplication : CompanyScopedEntity
{
    public Guid FacilityId { get; set; }
    public Guid SprayfieldId { get; set; }
    public DateTime ApplicationDate { get; set; }
    public decimal VolumeGallons { get; set; }
    public decimal? TimeIrrigatedMinutes { get; set; }
    public decimal NitrogenMgL { get; set; }
    public string? OperatorUserId { get; set; }
    public string OperatorSnapshotName { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;

    public Facility? Facility { get; set; }
    public Sprayfield? Sprayfield { get; set; }
    public ApplicationUser? OperatorUser { get; set; }
    public ICollection<LoadCalculation> LoadCalculations { get; set; } = new List<LoadCalculation>();
}
