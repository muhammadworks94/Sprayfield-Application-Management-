namespace SAM.ViewModels.Dashboard;

public class DashboardViewModel
{
    public int TotalFacilities { get; set; }
    public int TotalSprayfields { get; set; }
    public decimal TotalSprayfieldAcres { get; set; }
    public int TotalMonitoringWells { get; set; }
    public int TotalCompanies { get; set; }

    /// <summary>Number of irrigation events in last 10 (for "Recent Activity" KPI card).</summary>
    public int RecentActivityCount { get; set; }

    public int PendingUserRequests { get; set; }
    public int PendingAdminRequests { get; set; }

    public int RecentOperatorLogs { get; set; }
    public int RecentIrrigations { get; set; }
    public int RecentWWCharRecords { get; set; }
    public int RecentGWMonitRecords { get; set; }

    public int CompliantReports { get; set; }
    public int NonCompliantReports { get; set; }
    public int UnderReviewReports { get; set; }

    public List<RecentActivityViewModel> RecentActivities { get; set; } = new();
    public List<ComplianceSummaryViewModel> ComplianceSummaries { get; set; } = new();
    public List<SystemStatusViewModel> SystemStatuses { get; set; } = new();

    // Chart data: Irrigation Compliance (single pie)
    public IrrigationCompliancePieViewModel IrrigationCompliance { get; set; } = new();

    // Chart data: Operational Efficiency (system uptime + bar by date)
    public double SystemUptimePercent { get; set; }
    public List<OperationalEfficiencyPointViewModel> OperationalEfficiencyByDate { get; set; } = new();

    // Chart data: Wastewater Monitoring Trends (line: Avg BOD5, Avg TSS by period)
    public List<WastewaterTrendPointViewModel> WastewaterTrends { get; set; } = new();

    // Chart data: Groundwater Quality Overview (avg pH, avg conductivity, values by well)
    public GroundwaterOverviewViewModel GroundwaterOverview { get; set; } = new();
}

public class IrrigationCompliancePieViewModel
{
    public int CompliantCount { get; set; }
    public double CompliantPercent { get; set; }
    public int UnderReviewCount { get; set; }
    public double UnderReviewPercent { get; set; }
    public int NonCompliantCount { get; set; }
    public double NonCompliantPercent { get; set; }
}

public class OperationalEfficiencyPointViewModel
{
    public string DateLabel { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Status { get; set; } = "Normal";
}

public class WastewaterTrendPointViewModel
{
    public string PeriodLabel { get; set; } = string.Empty;
    public decimal? AvgBOD5 { get; set; }
    public decimal? AvgTSS { get; set; }
}

public class GroundwaterOverviewViewModel
{
    public decimal? AvgPH { get; set; }
    public decimal? AvgConductivity { get; set; }
    public List<GroundwaterWellPointViewModel> ByWell { get; set; } = new();
}

public class GroundwaterWellPointViewModel
{
    public string WellId { get; set; } = string.Empty;
    public decimal? PH { get; set; }
}

public class RecentActivityViewModel
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string User { get; set; } = string.Empty;
}

public class ComplianceSummaryViewModel
{
    public string FacilityName { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string ComplianceStatus { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
}

public class SystemStatusViewModel
{
    public string SystemName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    /// <summary>True for Normal/Operational (dark tag), false for Attention Required (light tag).</summary>
    public bool IsNormalOrOperational { get; set; }
}


