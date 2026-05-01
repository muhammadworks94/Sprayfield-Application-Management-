using SAM.Domain.Enums;

namespace SAM.Domain.Extensions;

public static class PermitTemplateDisplayExtensions
{
    public static string ToDisplayLabel(this MeasurementFrequencyEnum frequency)
    {
        return frequency switch
        {
            MeasurementFrequencyEnum.ThreeTimesPerYear => "3 x Year",
            _ => frequency.ToString()
        };
    }
}

