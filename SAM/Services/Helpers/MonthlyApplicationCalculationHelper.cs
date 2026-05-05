using SAM.Domain.Entities;
using SAM.Utilities;

namespace SAM.Services.Helpers;

public static class MonthlyApplicationCalculationHelper
{
    public const decimal GallonsPerAcreInch = 27154m;

    public static decimal ResolveAcres(Sprayfield sprayfield)
    {
        return SprayfieldReportHelper.GetReportAcres(sprayfield);
    }

    public static decimal? ComputeDailyLoadingInches(decimal? timeIrrigatedMinutes, decimal maxHourlyInchesPerHour)
    {
        if (!timeIrrigatedMinutes.HasValue || timeIrrigatedMinutes.Value <= 0m)
        {
            return null;
        }

        return maxHourlyInchesPerHour * (timeIrrigatedMinutes.Value / 60m);
    }

    public static decimal ComputeVolumeGallons(decimal dailyLoadingInches, decimal acres)
    {
        return dailyLoadingInches * acres * GallonsPerAcreInch;
    }

    public static decimal? ComputeDailyLoadingFromVolume(decimal volumeGallons, decimal acres)
    {
        if (acres <= 0m)
        {
            return null;
        }

        return volumeGallons / (acres * GallonsPerAcreInch);
    }
}
