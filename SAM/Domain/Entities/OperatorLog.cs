using SAM.Domain.Entities.Base;
using SAM.Domain.Enums;

namespace SAM.Domain.Entities;

/// <summary>
/// Daily log entries by operators detailing system status, maintenance, and issues.
/// </summary>
public class OperatorLog : CompanyScopedEntity
{
    /// <summary>
    /// Reference to the Facility entity.
    /// </summary>
    public Guid FacilityId { get; set; }

    /// <summary>
    /// Date of the log entry.
    /// </summary>
    public DateTime LogDate { get; set; }

    /// <summary>
    /// Name of the operator.
    /// </summary>
    public string OperatorName { get; set; } = string.Empty;

    /// <summary>
    /// Weather conditions.
    /// </summary>
    public string WeatherConditions { get; set; } = string.Empty;

    /// <summary>
    /// 24-hour arrival time of the operator.
    /// </summary>
    public TimeSpan ArrivalTime { get; set; }

    /// <summary>
    /// Total time spent on site for this log (in hours).
    /// </summary>
    public decimal TimeOnSiteHours { get; set; }

    /// <summary>
    /// Details of maintenance performed.
    /// </summary>
    public string MaintenancePerformed { get; set; } = string.Empty;

    /// <summary>
    /// Details of equipment inspected.
    /// </summary>
    public string EquipmentInspected { get; set; } = string.Empty;

    /// <summary>
    /// Issues noted.
    /// </summary>
    public string IssuesNoted { get; set; } = string.Empty;

    /// <summary>
    /// Corrective actions taken.
    /// </summary>
    public string CorrectiveActions { get; set; } = string.Empty;

    /// <summary>
    /// Notes for the next shift.
    /// </summary>
    public string NextShiftNotes { get; set; } = string.Empty;

    // Navigation properties
    public Facility? Facility { get; set; }
}


