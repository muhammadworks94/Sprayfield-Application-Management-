using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

/// <summary>
/// Zone-level sprayfield partition for calculations.
/// </summary>
public class ApplicationZone : CompanyScopedEntity
{
    public Guid SprayfieldId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public decimal PercentOfField { get; set; }
    public decimal Acres { get; set; }
    public Guid SoilId { get; set; }
    public Guid NozzleId { get; set; }
    public Guid? CropId { get; set; }
    public bool Active { get; set; } = true;

    public Sprayfield? Sprayfield { get; set; }
    public Soil? Soil { get; set; }
    public Nozzle? Nozzle { get; set; }
    public Crop? Crop { get; set; }
    public ICollection<MonthlyApplication> MonthlyApplications { get; set; } = new List<MonthlyApplication>();
}
