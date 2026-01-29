namespace SAM.ViewModels.Dashboard;

public class DashboardViewModel
{
    public int TotalFacilities { get; set; }
    public int TotalSprayfields { get; set; }
    public int TotalMonitoringWells { get; set; }
    public int TotalCompanies { get; set; }
    
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


