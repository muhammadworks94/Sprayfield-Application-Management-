using SAM.Domain.Entities;

namespace SAM.Utilities;

/// <summary>
/// GW-59 well-static fields stored on MonitoringWell and edited via GWMonit create/edit proxy forms.
/// </summary>
public sealed class Gw59WellDataFields
{
    public decimal? WellDepthFeet { get; set; }
    public decimal? DiameterInches { get; set; }
    public decimal? ScreenedIntervalFromFeet { get; set; }
    public decimal? ScreenedIntervalToFeet { get; set; }
    public decimal? MeasuringPointAboveLandSurface { get; set; }
    public decimal? RelativeMpElevation { get; set; }

    public static Gw59WellDataFields FromMonitoringWell(MonitoringWell? well)
    {
        if (well == null)
        {
            return new Gw59WellDataFields();
        }

        return new Gw59WellDataFields
        {
            WellDepthFeet = well.WellDepthFeet,
            DiameterInches = well.DiameterInches,
            ScreenedIntervalFromFeet = well.ScreenedIntervalFromFeet ?? well.LowScreenDepthFeet,
            ScreenedIntervalToFeet = well.ScreenedIntervalToFeet ?? well.HighScreenDepthFeet,
            MeasuringPointAboveLandSurface = well.MeasuringPointAboveLandSurface,
            RelativeMpElevation = well.RelativeMpElevation ?? well.TopOfCasingElevationMsl
        };
    }

    public void ApplyTo(MonitoringWell well)
    {
        well.WellDepthFeet = WellDepthFeet;
        well.DiameterInches = DiameterInches;
        well.ScreenedIntervalFromFeet = ScreenedIntervalFromFeet;
        well.ScreenedIntervalToFeet = ScreenedIntervalToFeet;
        well.MeasuringPointAboveLandSurface = MeasuringPointAboveLandSurface;
        well.RelativeMpElevation = RelativeMpElevation;
    }
}
