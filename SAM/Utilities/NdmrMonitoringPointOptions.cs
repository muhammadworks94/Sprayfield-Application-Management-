using SAM.Domain.Enums;

namespace SAM.Utilities;

public static class NdmrMonitoringPointOptions
{
    public static IReadOnlyList<(string Text, bool Selected)> BuildFlowOptions(FlowMeasuringPointEnum? selected)
    {
        return new List<(string Text, bool Selected)>
        {
            ("Influent", selected == FlowMeasuringPointEnum.Influent),
            ("Effluent", selected == FlowMeasuringPointEnum.Effluent),
            ("No flow generated", selected == FlowMeasuringPointEnum.NoFlowGenerated)
        };
    }

    public static IReadOnlyList<(string Text, bool Selected)> BuildParameterOptions(ParameterMonitoringPointEnum? selected)
    {
        return new List<(string Text, bool Selected)>
        {
            ("Influent", selected == ParameterMonitoringPointEnum.Influent),
            ("Effluent", selected == ParameterMonitoringPointEnum.Effluent),
            ("Groundwater Lowering", selected == ParameterMonitoringPointEnum.GroundwaterLowering),
            ("Surface Water", selected == ParameterMonitoringPointEnum.SurfaceWater)
        };
    }
}
