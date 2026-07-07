using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

/// <summary>
/// Client-setup baseline monthly loading (inches) for a sprayfield before operational data exists.
/// Ignored for a calendar month once real MonthlyApplications volume is recorded.
/// </summary>
public class SprayfieldBaselineMonthlyLoading : CompanyScopedEntity
{
    public Guid FacilityId { get; set; }
    public Guid SprayfieldId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal LoadingInches { get; set; }

    public Facility? Facility { get; set; }
    public Sprayfield? Sprayfield { get; set; }
}
