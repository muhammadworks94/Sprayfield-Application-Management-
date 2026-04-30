using SAM.Services.Interfaces;
using SAM.ViewModels.Admin;
using SAM.Domain.Entities;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SAM.Services.Implementations;

public class ProjectKnowledgeService : IProjectKnowledgeService
{
    public ProjectKnowledgeViewModel Build()
    {
        var model = new ProjectKnowledgeViewModel
        {
            Summary = "This page explains how SAM is structured, how major workflows run, how reports are generated, and which formulas and fallback rules are applied."
        };

        model.Sections.Add(BuildSystemOverview());
        model.Sections.Add(BuildDataModelMap());
        model.Sections.Add(BuildOperationalFlows());
        model.Sections.Add(BuildReportLogic());
        model.Sections.Add(BuildTraceability());
        model.Sections.Add(BuildFallbacks());
        model.Sections.Add(BuildTroubleshooting());

        model.MermaidDiagrams.Add("""
flowchart LR
    A[System Admin Setup] --> B[Facility Sprayfield Permit Versions]
    B --> C[Permit Template PCS Rows]
    C --> D[Operational Entry]
    D --> E[Monthly Applications]
    D --> F[WWChar]
    D --> G[GW Monitoring]
    E --> H[Compliance Validation]
    F --> H
    G --> I[NDMR and NDMLR]
    E --> J[NDAR1]
    F --> I
""");

        model.MermaidDiagrams.Add("""
flowchart TD
    Company --> Facility
    Facility --> Sprayfield
    Facility --> MonitoringWell
    Facility --> MonthlyApplication
    Facility --> WWChar
    Facility --> GWMonit
    Facility --> NDAR1
    Facility --> IrrRprt
    Facility --> FacilityPermit
    FacilityPermit --> FacilityPermitTemplateParameter
    FacilityPermitTemplateParameter --> WWCharTemplateValue
    FacilityPermitTemplateParameter --> GWMonitTemplateValue
    PcsParameterCatalog --> FacilityPermitTemplateParameter
""");

        model.MermaidDiagrams.Add("""
flowchart TD
    A[Facility and Report Date] --> B{Find active permit for date}
    B -- No --> C[No active permit message]
    B -- Yes --> D{NDMR template rows exist}
    D -- No --> E[No template rows message]
    D -- Yes --> F[Use permit version and template for report entry]
""");

        var traceability = model.Sections
            .SelectMany(s => s.TraceabilityItems)
            .ToList();

        model.TraceabilityEntityFilters = traceability
            .Select(x => x.Entity)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        model.TraceabilityReportFilters = traceability
            .SelectMany(x => x.UsedByReports)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        return model;
    }

    private static ProjectSectionViewModel BuildSystemOverview()
    {
        return new ProjectSectionViewModel
        {
            Id = "system-overview",
            Title = "System Overview",
            Intro = "SAM tracks wastewater and irrigation operations, calculates compliance values, and generates reports for regulatory workflows.",
            Cards =
            {
                new ProjectInfoCardViewModel
                {
                    Heading = "Main Modules",
                    Bullets =
                    {
                        "System Administration: master setup (facilities, sprayfields, permits, lookup entities).",
                        "Operational Data: monthly applications, WWChar, GW monitoring, operator logs.",
                        "Reports: NDAR1, NDMR/NDMLR exports, irrigation reports.",
                        "Analytics and Dashboard: trends and status summaries."
                    }
                }
            },
            Tables =
            {
                new ProjectTableViewModel
                {
                    Caption = "Role Access (simplified)",
                    Headers = { "Role", "Typical Access" },
                    Rows =
                    {
                        new List<string> { "admin", "All modules + global admin tools + cross-company context" },
                        new List<string> { "company_admin", "Company-scoped operations, reports, user management" },
                        new List<string> { "technician", "Wastewater/GW data and system administration entities" },
                        new List<string> { "operator", "Monthly applications, operator logs, system administration entities" }
                    }
                }
            }
        };
    }

    private static ProjectSectionViewModel BuildDataModelMap()
    {
        return new ProjectSectionViewModel
        {
            Id = "data-model-map",
            Title = "Core Data Model Map",
            Intro = "These are the core entities and how they relate for setup, operational data, and report generation.",
            Tables =
            {
                new ProjectTableViewModel
                {
                    Caption = "Core Entities and Usage",
                    Headers = { "Entity", "Purpose", "Key Linked Entities", "Used By" },
                    Rows =
                    {
                        new List<string> { "Company", "Top-level tenant scope", "Facility, Users", "All modules" },
                        new List<string> { "Facility", "Primary operating site", "Sprayfield, MonitoringWell, MonthlyApplication, WWChar, GWMonit, FacilityPermit", "Operations + Reports" },
                        new List<string> { "Sprayfield", "Irrigation field setup", "Facility, Crop, Nozzle, Soil", "Monthly Apps, NDAR1 calculations" },
                        new List<string> { "FacilityPermit", "Permit version + date range + PDF", "Facility, FacilityPermitTemplateParameter", "WWChar template resolution, NDMR header logic" },
                        new List<string> { "FacilityPermitTemplateParameter", "Permit-bound PCS config", "PcsParameterCatalog, WWCharTemplateValue, GWMonitTemplateValue", "Dynamic template-driven behavior" },
                        new List<string> { "WWChar", "Monthly wastewater chemistry base record", "Facility, FacilityPermit, WWCharTemplateValue", "Monthly compliance checks, NDMR input" },
                        new List<string> { "GWMonit", "Groundwater monitoring record", "Facility, MonitoringWell, GWMonitTemplateValue", "NDMR/NDMLR related workflows" },
                        new List<string> { "MonthlyApplication", "Sprayfield-level monthly application record", "Facility, Sprayfield", "Compliance + NDAR1 inputs" },
                        new List<string> { "NDAR1", "Non-discharge report data model", "Facility, Sprayfields, dynamic field rows", "NDAR1 report screens/exports" }
                    }
                }
            },
            Cards =
            {
                new ProjectInfoCardViewModel
                {
                    Heading = "Soft Delete and Active Flags",
                    Bullets =
                    {
                        "Most entities use soft-delete via IsDeleted and global query filters.",
                        "FacilityPermit also uses IsActive for permit resolution by date.",
                        "Archived permit = IsActive false (still retained). Deleted permit = IsDeleted true."
                    }
                }
            }
        };
    }

    private static ProjectSectionViewModel BuildOperationalFlows()
    {
        return new ProjectSectionViewModel
        {
            Id = "operational-flows",
            Title = "Operational Data Flows",
            Intro = "Operational entry is where source data is captured before reports are generated.",
            Flows =
            {
                new ProjectStepFlowViewModel
                {
                    Name = "Monthly Applications",
                    Steps =
                    {
                        "User selects facility/sprayfield/date and enters Daily Loading (inches).",
                        "System computes Volume (gallons) = DailyLoading * SprayfieldArea * 27,154.",
                        "Maximum Hourly Loading is sourced from sprayfield annual-rate field (current configured behavior).",
                        "Compliance endpoint validates WWChar dependency (including TKN) for same month/year."
                    }
                },
                new ProjectStepFlowViewModel
                {
                    Name = "WWChar",
                    Steps =
                    {
                        "User selects facility/month/year.",
                        "System resolves active permit version for date.",
                        "System loads permit template PCS rows flagged for NDMR.",
                        "If none found, page shows guided status message describing missing setup.",
                        "Values save both legacy WWChar context and template value rows."
                    }
                },
                new ProjectStepFlowViewModel
                {
                    Name = "GW Monitoring",
                    Steps =
                    {
                        "User records groundwater measurements tied to facility/well/date.",
                        "Template foundation exists for PCS-based mapping (v1 hooks in place).",
                        "Used downstream for report exports and compliance context."
                    }
                },
                new ProjectStepFlowViewModel
                {
                    Name = "Operator Logs",
                    Steps =
                    {
                        "Daily/period operational observations are captured.",
                        "These records support operational traceability and analytics."
                    }
                }
            }
        };
    }

    private static ProjectSectionViewModel BuildReportLogic()
    {
        return new ProjectSectionViewModel
        {
            Id = "report-logic",
            Title = "Report Logic and Formulas",
            Intro = "This section documents key formulas, data sources, and fallback behavior used by report services.",
            Tables =
            {
                new ProjectTableViewModel
                {
                    Caption = "Key Formulas and Constants",
                    Headers = { "Logic", "Formula", "Notes" },
                    Rows =
                    {
                        new List<string> { "Monthly App Volume", "Volume(gal) = DailyLoading(in) * Area(acres) * 27,154", "Used in monthly application create/edit flow." },
                        new List<string> { "NDAR Daily Loading", "DailyLoading(in) = Volume(gal) / (Area(acres) * 27,152)", "NDAR service constant currently uses 27,152." },
                        new List<string> { "Max Hourly (< 60 min)", "MaxHourlyLoading = DailyLoading", "If irrigated time < 60." },
                        new List<string> { "Max Hourly (>= 60 min)", "MaxHourlyLoading = (DailyLoading / TimeIrrigatedMin) * 60", "If irrigated time >= 60." },
                        new List<string> { "Monthly Loading", "Sum of daily loading values", "Per field." },
                        new List<string> { "12-Month Floating Total", "Current month monthly loading + previous 11 months monthly loading", "Per field." }
                    }
                },
                new ProjectTableViewModel
                {
                    Caption = "Report Source Mapping",
                    Headers = { "Report", "Primary Source Models", "Header/Permit Logic", "Fallback Notes" },
                    Rows =
                    {
                        new List<string> { "NDAR1", "NDAR1 + NDAR1Field + NDAR1FieldDaily + MonthlyApplication", "Facility permit context influences setup and source values", "Area/time/value handling follows per-field formula rules." },
                        new List<string> { "NDMR", "WWChar + GWMonit + permit template PCS rows", "Permit number/version resolved by facility + date when available", "If no template rows, service falls back to legacy defaults in current implementation path." },
                        new List<string> { "Irrigation Report", "Monthly applications + supporting operational context", "Facility metadata", "Compliance status and summary metrics derive from source entries." },
                        new List<string> { "GW report outputs", "GWMonit (+ template hooks)", "Facility/well context", "Template-driven extension path in progress." }
                    }
                }
            }
        };
    }

    private static ProjectSectionViewModel BuildTraceability()
    {
        var section = new ProjectSectionViewModel
        {
            Id = "property-traceability",
            Title = "Property Traceability Matrix",
            Intro = "Search any property (like Address, BOD5, TKN, 50050, DailyLoading) to see where it is stored, transformed, and used in reports.",
            TraceabilityItems =
            {
                new TraceabilityItemViewModel
                {
                    Id = "trace-daily-loading-ndar",
                    KeywordOrProperty = "DailyLoading",
                    Aliases = { "Daily Loading", "in/ac", "NDAR daily loading" },
                    Entity = "NDAR1FieldDaily",
                    StorageField = "DailyLoading",
                    UsedInModule = "Reports > NDAR1",
                    FormulaOrTransformation = "DailyLoading(in) = Volume(gal) / (Area(acres) * 27,152)",
                    ReportOutput = "NDAR1 day rows and monthly totals per sprayfield",
                    FallbackOrValidation = "Requires sprayfield area; monthly totals aggregate daily rows.",
                    Reference = new TraceReferenceViewModel
                    {
                        Label = "NDAR1 Loading Logic",
                        Location = "Services/Implementations/NDAR1Service + Data/Configurations/NDAR1FieldDailyConfiguration.cs"
                    },
                    UsedByReports = { "NDAR1" },
                    Steps =
                    {
                        new TraceStepViewModel { Order = 1, Label = "Input Source", Detail = "Volume and time originate from operational records tied to report month." },
                        new TraceStepViewModel { Order = 2, Label = "Storage", Detail = "Per-day values stored in NDAR1FieldDaily.DailyLoading." },
                        new TraceStepViewModel { Order = 3, Label = "Computation", Detail = "Area conversion and time-based max loading are computed in NDAR service." },
                        new TraceStepViewModel { Order = 4, Label = "Report Surface", Detail = "Rendered in NDAR1 edit/details/export rows and rollups." }
                    },
                    Formulas =
                    {
                        new TraceFormulaViewModel
                        {
                            Name = "NDAR Daily Loading",
                            Expression = "DailyLoading(in) = Volume(gal) / (Area(acres) * 27,152)",
                            Notes = "NDAR conversion constant."
                        },
                        new TraceFormulaViewModel
                        {
                            Name = "Max Hourly split",
                            Expression = "<60 min => MaxHourly = DailyLoading; >=60 min => (DailyLoading / TimeMin) * 60",
                            Notes = "Applied by irrigation duration branch."
                        }
                    },
                    Usages =
                    {
                        new TraceUsageViewModel { Module = "Reports", Report = "NDAR1", Destination = "Daily grid, monthly loading, floating total." }
                    },
                    FallbackRules =
                    {
                        new TraceFallbackRuleViewModel { Condition = "Missing area", Behavior = "Cannot compute reliable daily loading; setup must be corrected." }
                    }
                },
                new TraceabilityItemViewModel
                {
                    Id = "trace-monthly-application-volume",
                    KeywordOrProperty = "VolumeGallons",
                    Aliases = { "Volume Applied", "Monthly App Volume", "27154" },
                    Entity = "MonthlyApplication",
                    StorageField = "VolumeGallons",
                    UsedInModule = "Operational Data > Monthly Applications",
                    FormulaOrTransformation = "Volume(gal) = DailyLoading(in) * ReportAcres * 27,154",
                    ReportOutput = "Feeds NDAR1 source records and monthly operational summaries",
                    FallbackOrValidation = "Save blocked when sprayfield area missing/zero; annual rate required for max hourly assignment.",
                    Reference = new TraceReferenceViewModel
                    {
                        Label = "Monthly Application Create/Edit",
                        Location = "Controllers/OperationalDataController.cs"
                    },
                    UsedByReports = { "NDAR1", "Irrigation" },
                    Steps =
                    {
                        new TraceStepViewModel { Order = 1, Label = "Input Source", Detail = "User enters Daily Loading in Monthly Application form." },
                        new TraceStepViewModel { Order = 2, Label = "Storage", Detail = "Server computes and stores VolumeGallons; client value is not trusted." },
                        new TraceStepViewModel { Order = 3, Label = "Computation", Detail = "Area uses sprayfield report acres and 27,154 constant." },
                        new TraceStepViewModel { Order = 4, Label = "Report Surface", Detail = "Used as live-source data for NDAR and operational reports." }
                    },
                    Formulas =
                    {
                        new TraceFormulaViewModel
                        {
                            Name = "Monthly App Volume",
                            Expression = "Volume(gal) = DailyLoading(in) * Acres * 27,154",
                            Notes = "Monthly application specific conversion."
                        }
                    },
                    Usages =
                    {
                        new TraceUsageViewModel { Module = "Operational Data", Report = "NDAR1", Destination = "NDAR row-source generation." },
                        new TraceUsageViewModel { Module = "Reports", Report = "Irrigation", Destination = "Applied volume summaries." }
                    },
                    FallbackRules =
                    {
                        new TraceFallbackRuleViewModel { Condition = "Acres missing/zero", Behavior = "Save blocked with validation message." },
                        new TraceFallbackRuleViewModel { Condition = "Annual rate missing", Behavior = "Save blocked; configure sprayfield annual rate first." }
                    }
                },
                new TraceabilityItemViewModel
                {
                    Id = "trace-bod5",
                    KeywordOrProperty = "BOD5",
                    Aliases = { "00310", "BOD", "BOD5Daily" },
                    Entity = "WWChar",
                    StorageField = "BOD5Daily",
                    UsedInModule = "Operational Data > WWChar",
                    FormulaOrTransformation = "Captured as daily array values (up to 31 days); aggregated by report consumers as needed",
                    ReportOutput = "NDMR wastewater parameter outputs (template-driven path)",
                    FallbackOrValidation = "Requires applicable permit version and template parameters for selected month/year.",
                    Reference = new TraceReferenceViewModel
                    {
                        Label = "WWChar Daily Chemistry",
                        Location = "Domain/Entities/WWChar.cs + Controllers/OperationalDataController.cs"
                    },
                    UsedByReports = { "NDMR" },
                    Steps =
                    {
                        new TraceStepViewModel { Order = 1, Label = "Input Source", Detail = "User enters BOD5 daily chemistry values in WWChar." },
                        new TraceStepViewModel { Order = 2, Label = "Storage", Detail = "Stored in WWChar.BOD5Daily and/or template value rows." },
                        new TraceStepViewModel { Order = 3, Label = "Computation", Detail = "Mapped through permit template parameter rows for NDMR context." },
                        new TraceStepViewModel { Order = 4, Label = "Report Surface", Detail = "Exported in NDMR parameter sheets/columns." }
                    },
                    Formulas =
                    {
                        new TraceFormulaViewModel
                        {
                            Name = "NDMR Parameter Mapping",
                            Expression = "PCS parameter rows bind entered WWChar values to NDMR output positions",
                            Notes = "Template-driven; not a single arithmetic formula."
                        }
                    },
                    Usages =
                    {
                        new TraceUsageViewModel { Module = "Operational Data", Report = "NDMR", Destination = "Wastewater chemistry sections." }
                    },
                    FallbackRules =
                    {
                        new TraceFallbackRuleViewModel { Condition = "No permit version active for date", Behavior = "Template rows unavailable; guided warning shown." },
                        new TraceFallbackRuleViewModel { Condition = "No NDMR template rows", Behavior = "Page warns to configure PCS rows under permit version." }
                    }
                },
                new TraceabilityItemViewModel
                {
                    Id = "trace-tkn",
                    KeywordOrProperty = "TKN",
                    Aliases = { "TKNN", "Kjeldahl", "00625", "81639" },
                    Entity = "WWChar / GWMonit",
                    StorageField = "WWChar.TKNN and GWMonit.TKN",
                    UsedInModule = "Operational Data > WWChar and GW Monitoring",
                    FormulaOrTransformation = "Used directly and in downstream nitrogen-related calculations/exports",
                    ReportOutput = "Compliance checks and NDMR/GW-related output context",
                    FallbackOrValidation = "Monthly Application compliance requires WWChar chemistry including TKN for same month/year.",
                    Reference = new TraceReferenceViewModel
                    {
                        Label = "TKN Validation + Monitoring",
                        Location = "Controllers/OperationalDataController.cs + ApplicationComplianceService"
                    },
                    UsedByReports = { "NDMR", "NDMLR/GW", "NDAR1 (compliance dependency)" },
                    Steps =
                    {
                        new TraceStepViewModel { Order = 1, Label = "Input Source", Detail = "Entered in WWChar and GW monitoring forms." },
                        new TraceStepViewModel { Order = 2, Label = "Storage", Detail = "Stored as TKNN in WWChar and TKN in GWMonit." },
                        new TraceStepViewModel { Order = 3, Label = "Validation", Detail = "Checked by compliance logic for monthly application prerequisites." },
                        new TraceStepViewModel { Order = 4, Label = "Report Surface", Detail = "Used in NDMR/GW outputs and monitoring context." }
                    },
                    Formulas =
                    {
                        new TraceFormulaViewModel
                        {
                            Name = "Compliance prerequisite",
                            Expression = "WWChar(TKN) must exist for application month/year",
                            Notes = "Blocking validation in monthly application workflow."
                        }
                    },
                    Usages =
                    {
                        new TraceUsageViewModel { Module = "Operational Data", Report = "NDMR", Destination = "Chemistry output fields." },
                        new TraceUsageViewModel { Module = "Operational Data", Report = "GW forms", Destination = "Groundwater chemistry fields." }
                    },
                    FallbackRules =
                    {
                        new TraceFallbackRuleViewModel { Condition = "Missing WWChar/TKN for month", Behavior = "Monthly application save is blocked until chemistry exists." }
                    }
                },
                new TraceabilityItemViewModel
                {
                    Id = "trace-flow-50050",
                    KeywordOrProperty = "Flow (PCS 50050)",
                    Aliases = { "50050", "Flow", "GPD" },
                    Entity = "PcsParameterCatalog + FacilityPermitTemplateParameter",
                    StorageField = "PcsCode=50050 template row (required)",
                    UsedInModule = "System Admin > Permit Template setup",
                    FormulaOrTransformation = "Template gate requires Flow row before permit template activation",
                    ReportOutput = "NDMR first parameter position / required output structure",
                    FallbackOrValidation = "Template publish/usage blocked when 50050 is missing.",
                    Reference = new TraceReferenceViewModel
                    {
                        Label = "Permit Template Requirement",
                        Location = "Permit Template validation/service logic"
                    },
                    UsedByReports = { "NDMR" },
                    Steps =
                    {
                        new TraceStepViewModel { Order = 1, Label = "Input Source", Detail = "Admin adds PCS 50050 in permit template parameters." },
                        new TraceStepViewModel { Order = 2, Label = "Storage", Detail = "Stored as permit-bound template row with required flag." },
                        new TraceStepViewModel { Order = 3, Label = "Validation", Detail = "Template enforce rule checks for required Flow presence." },
                        new TraceStepViewModel { Order = 4, Label = "Report Surface", Detail = "NDMR output uses Flow as required lead behavior." }
                    },
                    Formulas =
                    {
                        new TraceFormulaViewModel
                        {
                            Name = "Template gate",
                            Expression = "Permit template must include PCS 50050 Flow as required",
                            Notes = "Precondition for expected NDMR structure."
                        }
                    },
                    Usages =
                    {
                        new TraceUsageViewModel { Module = "System Admin", Report = "NDMR", Destination = "Required template parameter structure." }
                    },
                    FallbackRules =
                    {
                        new TraceFallbackRuleViewModel { Condition = "50050 missing", Behavior = "Template cannot be activated/used for compliant NDMR flow." }
                    }
                }
            }
        };

        section.TraceabilityItems.AddRange(BuildEntityPropertyCatalogTraceability());
        return section;
    }

    private static IEnumerable<TraceabilityItemViewModel> BuildEntityPropertyCatalogTraceability()
    {
        var items = new List<TraceabilityItemViewModel>();
        var types = typeof(Facility).Assembly
            .GetTypes()
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                t.Namespace == "SAM.Domain.Entities")
            .OrderBy(t => t.Name)
            .ToList();

        var moduleMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Facility"] = "System Administration > Facilities",
            ["Sprayfield"] = "System Administration > Sprayfields",
            ["MonitoringWell"] = "System Administration > Monitoring Wells",
            ["Soil"] = "System Administration > Soil Types",
            ["Crop"] = "System Administration > Crops",
            ["Nozzle"] = "System Administration > Nozzles",
            ["FacilityPermit"] = "System Administration > Permit Versions",
            ["FacilityPermitTemplateParameter"] = "System Administration > Permit Template",
            ["MonthlyApplication"] = "Operational Data > Monthly Applications",
            ["WWChar"] = "Operational Data > WWChar",
            ["WWCharTemplateValue"] = "Operational Data > WWChar (Template Values)",
            ["GWMonit"] = "Operational Data > GW Monitoring",
            ["GWMonitTemplateValue"] = "Operational Data > GW Monitoring (Template Values)",
            ["OperatorLog"] = "Operational Data > Operator Logs",
            ["NDAR1"] = "Reports > NDAR1",
            ["NDAR1Field"] = "Reports > NDAR1",
            ["NDAR1FieldDaily"] = "Reports > NDAR1",
            ["IrrRprt"] = "Reports > Irrigation"
        };

        var reportHints = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["MonthlyApplication"] = new() { "NDAR1", "Irrigation" },
            ["NDAR1"] = new() { "NDAR1" },
            ["NDAR1Field"] = new() { "NDAR1" },
            ["NDAR1FieldDaily"] = new() { "NDAR1" },
            ["WWChar"] = new() { "NDMR" },
            ["WWCharTemplateValue"] = new() { "NDMR" },
            ["GWMonit"] = new() { "NDMR", "NDMLR/GW" },
            ["GWMonitTemplateValue"] = new() { "NDMR", "NDMLR/GW" },
            ["FacilityPermit"] = new() { "NDMR", "NDAR1" },
            ["FacilityPermitTemplateParameter"] = new() { "NDMR" },
            ["Facility"] = new() { "NDAR1", "NDMR", "Irrigation", "NDMLR/GW" },
            ["Sprayfield"] = new() { "NDAR1", "Irrigation" }
        };

        foreach (var type in types)
        {
            var module = moduleMap.TryGetValue(type.Name, out var mappedModule)
                ? mappedModule
                : "System / Shared Model";
            var reports = reportHints.TryGetValue(type.Name, out var mappedReports)
                ? mappedReports
                : new List<string> { "Reference" };

            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).OrderBy(p => p.Name))
            {
                var propType = GetFriendlyTypeName(prop.PropertyType);
                var isNav = IsNavigationProperty(prop.PropertyType);
                var label = $"{type.Name}.{prop.Name}";
                var alias = SplitPascalCase(prop.Name);
                var idSafe = $"{type.Name}-{prop.Name}".ToLowerInvariant();

                items.Add(new TraceabilityItemViewModel
                {
                    Id = $"trace-entityprop-{idSafe}",
                    KeywordOrProperty = prop.Name,
                    Aliases = { label, alias, propType },
                    Entity = type.Name,
                    StorageField = label,
                    UsedInModule = module,
                    FormulaOrTransformation = isNav
                        ? "Navigation/relationship property (links related entities)"
                        : "Directly stored property; used by workflows and/or reports based on entity context",
                    ReportOutput = string.Join(", ", reports),
                    FallbackOrValidation = isNav
                        ? "Relationship resolution depends on linked records and active filters."
                        : "Depends on form validation and business rules in related workflows.",
                    Reference = new TraceReferenceViewModel
                    {
                        Label = "Entity Property Catalog",
                        Location = $"Domain/Entities/{type.Name}.cs"
                    },
                    UsedByReports = reports,
                    Steps =
                    {
                        new TraceStepViewModel { Order = 1, Label = "Source", Detail = $"Property is defined on entity {type.Name}." },
                        new TraceStepViewModel { Order = 2, Label = "Storage", Detail = $"{label} ({propType})." },
                        new TraceStepViewModel { Order = 3, Label = "Usage", Detail = $"Consumed in module path: {module}." },
                        new TraceStepViewModel { Order = 4, Label = "Reporting", Detail = $"Potential report surfaces: {string.Join(", ", reports)}." }
                    },
                    Formulas =
                    {
                        new TraceFormulaViewModel
                        {
                            Name = "Property Type",
                            Expression = propType,
                            Notes = isNav ? "Navigation relationship field." : "Scalar/data field."
                        }
                    },
                    Usages =
                    {
                        new TraceUsageViewModel
                        {
                            Module = module,
                            Report = string.Join(", ", reports),
                            Destination = isNav ? "Entity relationships and joins" : "Entity data usage and report context"
                        }
                    },
                    FallbackRules =
                    {
                        new TraceFallbackRuleViewModel
                        {
                            Condition = "Property value unavailable",
                            Behavior = "Rendered as blank/null-safe value depending on screen/report logic."
                        }
                    }
                });
            }
        }

        return items;
    }

    private static bool IsNavigationProperty(Type type)
    {
        if (type == typeof(string))
        {
            return false;
        }

        if (type.Namespace == "SAM.Domain.Entities")
        {
            return true;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>))
        {
            return true;
        }

        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            if (type.IsGenericType)
            {
                var arg = type.GetGenericArguments().FirstOrDefault();
                return arg?.Namespace == "SAM.Domain.Entities";
            }
        }

        return false;
    }

    private static string GetFriendlyTypeName(Type type)
    {
        var nullableType = Nullable.GetUnderlyingType(type);
        var coreType = nullableType ?? type;
        var typeName = coreType.Name switch
        {
            "String" => "string",
            "Int32" => "int",
            "Int64" => "long",
            "Decimal" => "decimal",
            "Boolean" => "bool",
            "DateTime" => "DateTime",
            "Guid" => "Guid",
            _ => coreType.Name
        };

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>))
        {
            var arg = type.GetGenericArguments()[0];
            return $"ICollection<{arg.Name}>";
        }

        return nullableType is null ? typeName : $"{typeName}?";
    }

    private static string SplitPascalCase(string input)
    {
        return Regex.Replace(input, "(\\B[A-Z])", " $1");
    }

    private static ProjectSectionViewModel BuildFallbacks()
    {
        return new ProjectSectionViewModel
        {
            Id = "fallbacks-and-errors",
            Title = "Fallbacks, Guardrails, and Error Meaning",
            Intro = "These are the core guardrails the system applies and how to resolve common setup/data issues.",
            Tables =
            {
                new ProjectTableViewModel
                {
                    Caption = "Fallback and Validation Matrix",
                    Headers = { "Condition", "System Behavior", "How to Fix" },
                    Rows =
                    {
                        new List<string> { "No permit versions for facility", "WWChar template section shows setup warning", "Add permit version in System Administration > Facilities > Permit Versions." },
                        new List<string> { "Permit versions exist but none active for month/year", "WWChar shows date-range warning", "Adjust effective dates or application/report period." },
                        new List<string> { "Permit active but no NDMR template rows", "WWChar shows template-row warning", "Add PCS rows under permit version template." },
                        new List<string> { "Permit unarchive causes overlap", "Unarchive blocked with message", "Adjust date ranges or archive conflicting active permit." },
                        new List<string> { "Duplicate permit number+version on create", "Create either restores soft-deleted match or shows friendly duplicate message", "Use archived section or update existing record." },
                        new List<string> { "PCS import duplicate codes", "Importer deduplicates and upserts safely", "Re-run import; duplicates in TSV no longer break process." }
                    }
                }
            }
        };
    }

    private static ProjectSectionViewModel BuildTroubleshooting()
    {
        return new ProjectSectionViewModel
        {
            Id = "troubleshooting",
            Title = "Troubleshooting and Change Impact",
            Intro = "Use these quick decision paths to find root causes without reading source code.",
            Flows =
            {
                new ProjectStepFlowViewModel
                {
                    Name = "Report value looks wrong",
                    Steps =
                    {
                        "Check source module first (Monthly App / WWChar / GWMonit) for the same period.",
                        "Verify facility and sprayfield setup values (area, rates, active flags).",
                        "Verify permit version resolved for report date and template rows used.",
                        "Re-check formula constants and time thresholds for the affected report."
                    }
                },
                new ProjectStepFlowViewModel
                {
                    Name = "Cannot generate expected report data",
                    Steps =
                    {
                        "Confirm user role has required access.",
                        "Confirm required operational records exist for month/year.",
                        "For WWChar/NDMR paths, confirm permit version + template PCS setup.",
                        "Check compliance warnings shown on entry pages; resolve prerequisites."
                    }
                }
            },
            Cards =
            {
                new ProjectInfoCardViewModel
                {
                    Heading = "Impact Reference",
                    Bullets =
                    {
                        "Changing sprayfield area/rates impacts monthly loading and NDAR outcomes.",
                        "Changing permit versions/date ranges impacts WWChar template resolution and NDMR context.",
                        "Changing permit template PCS rows impacts dynamic entry fields and NDMR parameter output.",
                        "Changing monthly application values impacts compliance projections and downstream reporting."
                    }
                }
            }
        };
    }
}
