namespace SAM.Services.Models;

public class ActivityLogOptions
{
    public const string SectionName = "ActivityLog";
    public int RetentionDays { get; set; } = 180;
    public int PurgeIntervalHours { get; set; } = 24;
}

