namespace SAM.ViewModels.Admin;

public class ProjectKnowledgeViewModel
{
    public string Title { get; set; } = "Project Knowledge Center";
    public string Summary { get; set; } = string.Empty;
    public List<ProjectSectionViewModel> Sections { get; set; } = new();
    public List<string> MermaidDiagrams { get; set; } = new();
    public List<string> TraceabilityEntityFilters { get; set; } = new();
    public List<string> TraceabilityReportFilters { get; set; } = new();
}

public class ProjectSectionViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Intro { get; set; } = string.Empty;
    public List<ProjectInfoCardViewModel> Cards { get; set; } = new();
    public List<ProjectTableViewModel> Tables { get; set; } = new();
    public List<ProjectStepFlowViewModel> Flows { get; set; } = new();
    public List<TraceabilityItemViewModel> TraceabilityItems { get; set; } = new();
}

public class ProjectInfoCardViewModel
{
    public string Heading { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public List<string> Bullets { get; set; } = new();
}

public class ProjectTableViewModel
{
    public string Caption { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
}

public class ProjectStepFlowViewModel
{
    public string Name { get; set; } = string.Empty;
    public List<string> Steps { get; set; } = new();
}

public class TraceabilityItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string KeywordOrProperty { get; set; } = string.Empty;
    public List<string> Aliases { get; set; } = new();
    public string Entity { get; set; } = string.Empty;
    public string StorageField { get; set; } = string.Empty;
    public string UsedInModule { get; set; } = string.Empty;
    public string FormulaOrTransformation { get; set; } = string.Empty;
    public string ReportOutput { get; set; } = string.Empty;
    public string FallbackOrValidation { get; set; } = string.Empty;
    public TraceReferenceViewModel Reference { get; set; } = new();
    public List<string> UsedByReports { get; set; } = new();
    public List<TraceStepViewModel> Steps { get; set; } = new();
    public List<TraceFormulaViewModel> Formulas { get; set; } = new();
    public List<TraceUsageViewModel> Usages { get; set; } = new();
    public List<TraceFallbackRuleViewModel> FallbackRules { get; set; } = new();
}

public class TraceStepViewModel
{
    public int Order { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

public class TraceFormulaViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class TraceUsageViewModel
{
    public string Module { get; set; } = string.Empty;
    public string Report { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
}

public class TraceFallbackRuleViewModel
{
    public string Condition { get; set; } = string.Empty;
    public string Behavior { get; set; } = string.Empty;
}

public class TraceReferenceViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}
