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
            Summary = "This page explains how SAM is structured, how major workflows run, how reports are generated, and which formulas and fallback rules are applied. Monthly NDAR-1, NDMR, and NDMLR report records can be auto-created when operational data is saved (bidirectional with manual Generate and NDAR grid edit). Groundwater monitoring uses permit-template PCS rows as the chemistry source of truth, combines GW-59 and GW-59A into one PDF export, and manages VOC attachments through the reports flow."
        };

        model.Sections.Add(BuildSystemOverview());
        model.Sections.Add(BuildDataModelMap());
        model.Sections.Add(BuildPermitTemplateGlossary());
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
    D --> O[Operator Logs]
    E --> H[Compliance Validation]
    F --> H
    G --> I[GW Reports List]
    E -->|auto-provision| J[NDAR1]
    E -->|auto-provision| L[NDMLR]
    O -->|auto-provision| J
    O -->|auto-provision| M[NDMR IrrRprt]
    F -->|auto-provision| M
    J -->|grid edit| D
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
    Facility --> NDMLR
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

        model.MermaidDiagrams.Add("""
flowchart TD
    subgraph triggers [Operational save triggers]
        MA[MonthlyApplication create/update]
        OL[OperatorLog create/update]
        WW[WWChar create/update]
        GR[NDAR1 grid row edit]
    end
    subgraph provisioner [MonthlyReportProvisionerService]
        P[Ensure for facility month/year]
    end
    subgraph reports [Persisted report records]
        N1[NDAR1]
        NDMR[IrrRprt NDMR]
        NDL[NDMLR]
    end
    MA --> P
    OL --> P
    WW --> P
    GR --> P
    P -->|missing: generate + create| N1
    P -->|existing: refresh computed fields| N1
    P -->|missing only| NDMR
    P -->|missing only| NDL
    MA -.->|also ensures| NDL
    OL -.->|also ensures| NDMR
    WW -.->|also ensures| NDMR
    GR -.->|ensures all three| N1
    GR -.-> NDMR
    GR -.-> NDL
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
                        "System Administration: master setup (facilities, permits tab, lab options, sprayfields, lookup entities).",
                        "Operational Data: monthly applications, WWChar, GW monitoring, operator logs.",
                        "Reports: NDAR1, NDMR, annual NDMLR exports, irrigation reports; monthly report records can auto-create from operational saves.",
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
                        new List<string> { "GWMonit", "Groundwater monitoring record", "Facility, MonitoringWell, GWMonitTemplateValue", "NDMR and annual NDMLR related workflows" },
                        new List<string> { "MonthlyApplication", "Sprayfield-level monthly application record", "Facility, Sprayfield", "Compliance + NDAR1 inputs; auto-provisions NDAR-1 + NDMLR for application month" },
                        new List<string> { "NDAR1", "Non-discharge report data model", "Facility, Sprayfields, dynamic field rows", "NDAR1 report screens/exports; auto-created/refreshed from application/operator logs and grid edit" },
                        new List<string> { "IrrRprt", "Monthly NDMR report identity and compliance summary", "Facility, Month, Year", "NDMR list/details/exports; auto-created from operator logs and WWChar" },
                        new List<string> { "NDMLR", "Annual non-discharge mass loading report identity", "Facility, Company, Year, Month (window end)", "NDMLR annual list/details/exports; auto-created from application logs" },
                        new List<string> { "OperatorLog", "Canonical per-day operations record", "Facility, date-level ORC On Site + Storage Lagoon Freeboard (ft)", "WWChar + NDAR proxy read/write source; auto-provisions NDAR-1 + NDMR for log month" }
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

    private static ProjectSectionViewModel BuildPermitTemplateGlossary()
    {
        return new ProjectSectionViewModel
        {
            Id = "permit-template-glossary",
            Title = "Permit Template and Attachment A/C Guide",
            Intro = "This quick guide explains key terms, what each permit template field means, and how Attachment A (wastewater) and Attachment C (groundwater) template rows are used in daily entry and reporting.",
            Cards =
            {
                new ProjectInfoCardViewModel
                {
                    Heading = "Plain-Language Definitions",
                    Bullets =
                    {
                        "PCS Catalog: Master list of regulatory parameter codes (for example 00310 BOD5, 50050 Flow) with names and units. Admin selects rows from this catalog when building a permit template.",
                        "Permit Number: The regulatory permit identifier issued by the authority (for example WQ0004332).",
                        "Permit Version: Revision/reissue label of the same permit number (for example 4.1). Versions let us keep history when limits or requirements change.",
                        "Permit Effective Dates: Date range that determines which permit version applies to a given report month.",
                        "Attachment A (Limitations and Monitoring Requirements): Wastewater section defining limits, monitoring frequency, and sample type.",
                        "Attachment C (Groundwater Monitoring and Limitations): Groundwater section defining monitoring requirements, daily maximums, sample type, and special footnote instructions."
                    }
                },
                new ProjectInfoCardViewModel
                {
                    Heading = "How It Works in SAM",
                    Bullets =
                    {
                        "Admin creates a permit version and adds permit template parameter rows from PCS catalog.",
                        "Each row stores limits and monitoring requirements from Attachment A (WW) or Attachment C (GW), based on report section.",
                        "When WWChar is opened for a facility/month, system resolves active permit by date and loads wastewater template rows.",
                        "WWChar now filters periodic rows by selected month (for example 3 x Year / Annual rows only appear when ScheduledMonthsCsv includes that month; blank periodic schedules are hidden).",
                        "When GW Monitoring Create/Edit is opened for a facility/sample date, system resolves active permit and loads groundwater template rows.",
                        "Operator enters groundwater values directly in the template-entry column; SAM stores values tied to the exact permit template row.",
                        "Groundwater and wastewater template values are stored separately (GWMonitTemplateValues vs WWCharTemplateValues) even when sharing the same PCS catalog entry (for example pH).",
                        "Rows required for the selected sample month are highlighted and enforced at save with user-friendly unblock guidance.",
                        "PAN chemistry in WWChar workflow is sourced from template PCS rows: 00625 (TKN) and 00620 (NO3). NO2 is treated as 0 for PAN.",
                        "Groundwater chemistry previously stored as direct TDS/Turbidity fields was cut over to permit-template PCS values; GW reporting now resolves chemistry from saved GWMonitTemplateValues.",
                        "NDMR/GW reporting uses the same permit/template context so output matches configured permit requirements.",
                        "Per-row notes (permit instructions/footnotes) are shown through an info icon tooltip in admin and monitoring dialogs.",
                        "GW VOC report attachment is PDF-only, validated with the same PDF parser used by report merge, and is merged into the combined GW-59 export when present."
                    }
                }
            },
            Tables =
            {
                new ProjectTableViewModel
                {
                    Caption = "Permit Template Property Dictionary (Attachment A + C fields)",
                    Headers = { "Property", "Definition", "How SAM Uses It" },
                    Rows =
                    {
                        new List<string> { "PcsParameterCatalogId", "Selected PCS parameter from master catalog.", "Determines which parameter row appears (code/name/units baseline)." },
                        new List<string> { "ParameterDisplayOverride", "Optional custom label for parameter.", "Overrides catalog name in UI/report context where template rows are shown." },
                        new List<string> { "UnitsOverride", "Optional custom units text.", "Overrides catalog units for display and user clarity." },
                        new List<string> { "MonthlyAverageLimit", "Maximum/target monthly average limit from permit.", "Displayed to operators as reference during WWChar entry; supports permit-context review." },
                        new List<string> { "MonthlyGeometricMeanLimit", "Monthly geometric mean limit from permit.", "Displayed in WWChar template grid to guide entry and compliance awareness." },
                        new List<string> { "DailyMinimumLimit", "Permit-required daily minimum threshold.", "Displayed in WWChar template grid as lower-bound reference." },
                        new List<string> { "DailyMaximumLimit", "Permit-required daily maximum threshold.", "Displayed in admin + WW/GW monitoring grids; commonly used quick compliance reference." },
                        new List<string> { "MeasurementFrequency", "How often sampling/measurement must occur (Daily/Weekly/Monthly/3 x Year/etc.).", "Shown in WWChar/GW template rows and exports using user-friendly labels." },
                        new List<string> { "SampleType", "Required sampling method (Grab/Composite/Recorder/Calculated).", "Shown in WWChar template rows so recorded data matches permit method." },
                        new List<string> { "ScheduledMonthsCsv", "Optional month numbers for periodic sampling (for example 4,8,11 for Apr/Aug/Nov).", "Used with periodic frequencies (for example 3 x Year, Annual). Not needed for Monthly." },
                        new List<string> { "Notes", "Optional permit instructions/footnote text for that row.", "Shown in info-icon tooltip in Permit Versions and WW/GW monitoring template dialogs." },
                        new List<string> { "SortOrder", "Display order of rows in template.", "Controls row order in admin and WWChar dynamic grid." },
                        new List<string> { "IsRequired", "Marks parameter as required for template workflow.", "Highlights required rows and supports rule-driven report readiness checks." },
                        new List<string> { "ReportTypes", "Flags that indicate which report flows this row applies to (NDMR/NVMR/GW59/GW59A).", "Filters which rows load into each operational/report workflow." }
                    }
                },
                new ProjectTableViewModel
                {
                    Caption = "Common Examples",
                    Headers = { "Permit Text", "Stored Value in SAM", "Notes" },
                    Rows =
                    {
                        new List<string> { "3 x Year (Apr, Aug, Nov)", "MeasurementFrequency = ThreeTimesPerYear, ScheduledMonthsCsv = 4,8,11", "Display label is \"3 x Year\" while enum storage stays ThreeTimesPerYear." },
                        new List<string> { "Monthly", "MeasurementFrequency = Monthly, ScheduledMonthsCsv = null", "Monthly means every month; Months CSV is not required." },
                        new List<string> { "Attachment C footnote instruction", "Notes = [instruction text]", "Displayed in info-icon tooltip in admin + GW monitoring dialog." },
                        new List<string> { "Sample Type: Grab", "SampleType = Grab", "Shown in WWChar row for operator guidance." },
                        new List<string> { "Daily Max limit provided", "DailyMaximumLimit = [permit value]", "Displayed as read-only reference in WWChar template grid." }
                    }
                }
            },
            Flows =
            {
                new ProjectStepFlowViewModel
                {
                    Name = "Quick Setup Checklist",
                    Steps =
                    {
                        "Create/verify permit version with correct permit number, version, and effective dates.",
                        "Add Attachment A (wastewater) and Attachment C (groundwater) PCS rows under that permit version.",
                        "For each row, set limits, frequency, sample type, notes, and months CSV only when periodic schedule applies.",
                        "Do not enter NO2/TKN/NO3 in WWChar form fields; chemistry is now derived from Attachment A template rows.",
                        "Confirm required Flow row (PCS 50050) exists for NDMR flow.",
                        "Open WWChar (for month/year) and GW Monitoring (for sample date) to verify expected rows/limits/notes appear."
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
            Intro = "Operational entry captures source data and can auto-create monthly report records. Manual Generate on Reports screens still works and is idempotent when a report already exists.",
            Flows =
            {
                new ProjectStepFlowViewModel
                {
                    Name = "Monthly Applications",
                    Steps =
                    {
                        "User selects facility/sprayfield/date and enters Time Irrigated (minutes).",
                        "Maximum Hourly Loading is sourced from System Admin > Sprayfield > Permitted (Max) Hourly Rate.",
                        "System computes Daily Loading (inches) = MaximumHourlyLoading * (TimeIrrigatedMinutes / 60).",
                        "System computes Volume (gallons) = DailyLoading * SprayfieldArea * 27,154.",
                        "Compliance endpoint validates WWChar chemistry dependency for same month/year (TKN/NO3 sourced from template PCS 00625/00620).",
                        "On successful create/update, MonthlyReportProvisionerService ensures NDAR-1 (create or refresh) and NDMLR (create if missing) for the application month/year."
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
                        "Chemistry/template entry values are saved on WWChar + WWCharTemplateValue rows.",
                        "ORC On Site, Storage Lagoon Freeboard (ft), ORC Arrival Time, and ORC Time on Site (hours) are proxy fields: WWChar reads/writes these values through Operator Logs for each exact date.",
                        "If a day value is edited in WWChar and no Operator Log exists for that date, SAM creates a log record and saves canonical values there.",
                        "After WWChar + template values are saved, MonthlyReportProvisionerService ensures NDMR (IrrRprt) exists for that facility/month/year (create only if missing)."
                    }
                },
                new ProjectStepFlowViewModel
                {
                    Name = "GW Monitoring",
                    Steps =
                    {
                        "User records groundwater monitoring tied to facility/well/date.",
                        "System resolves active permit and shows Attachment C template rows with editable value inputs.",
                        "GW saves reject template rows outside GW59/GW59A for the resolved permit to prevent cross-report contamination.",
                        "Rows required for the selected sample month (from frequency + scheduled months) must be entered before save.",
                        "GW-59 well-static fields (well depth, diameter, screened interval from/to, measuring point, relative M.P. elevation) are stored on MonitoringWell and can be edited from GWMonit create/edit as a proxy that updates the selected well master record.",
                        "Per-sample GWMonit fields include water level, gallons pumped, field observations, template PCS values, and optional metals Y/N (MetalsSamplesCollectedUnfiltered, MetalSamplesFieldAcidified).",
                        "Metals Y/N are optional on save; unset values export as blank yes/no boxes on GW-59 PDF (same behavior as GW-59A unanswered questions).",
                        "GW-59A compliance answers (Q1-Q7, detail text, due date, signer/date) are captured on GWMonit create/edit as a separate questionnaire workflow.",
                        "If Attachment C row 78732 (Volatile Compounds) is required for the selected month, save requires a VOC PDF, forces VOC Report Attached to true, and requires VOC Method #.",
                        "Unchecking VOC Report Attached no longer deletes the stored VOC file; replacement uploads still remove the old blob and overwrite the groundwater record metadata.",
                        "Edit flow provides an explicit remove-current-VOC-file action, which clears the stored blob and forces VOC Report Attached off unless a replacement file is uploaded.",
                        "Template values are stored and used downstream for reporting/compliance context."
                    }
                },
                new ProjectStepFlowViewModel
                {
                    Name = "Groundwater Quality Reports",
                    Steps =
                    {
                        "Company Admin opens Reports > Groundwater Quality Reports.",
                        "System lists groundwater monitoring records grouped by sample date (toggleable) with facility, well, and resolved permit context.",
                        "List toolbar provides group-by-date toggle (default on), page size selector, and record count summary; pagination is always visible.",
                        "When group-by-date is off, the list renders as a flat table without date headers or per-date bulk download.",
                        "Each sample-date group offers Download all PDFs, which merges every filtered record for that date into one PDF (GW-59 + GW-59A + VOC per record). Individual row Download PDF actions remain available.",
                        "Download PDF is the single supported groundwater report export action; the record-details page no longer serves as the report-download entry point.",
                        "ExportGW59Report generates one combined PDF in this order: GW-59, GW-59A if questionnaire data exists, then attached VOC PDF pages if present.",
                        "GW-59 export uses letter-size form template GW59_Resized.pdf (792×612 pt, 11\"×8.5\" landscape) with mapped facility, permit, monitoring-well, GWMonit, and permit-template data.",
                        "GW-59 Facility Information section maps Sys Admin facility/permit/well master data through Gw59FacilityFieldResolver (Utilities/Gw59FacilityFieldResolver.cs) for export and preview parity.",
                        "GW-59 Contact Person exports Facility.FacilityContactPerson.",
                        "GW-59 Telephone exports Facility.FacilityContactPersonPhone.",
                        "GW-59 Well Location / Site Name exports MonitoringWell.LocationDescription for the well selected on the groundwater record.",
                        "GW-59 No. of wells to be sampled counts active MonitoringWell rows for the facility company (Sys Admin > Monitoring Wells).",
                        "GW-59 preview (Operational Data > GW Monit > Preview GW-59) and Reports > Groundwater Quality Reports > Download PDF both use the same BuildGw59ExportModelAsync / BuildGW59ReportAsync resolver logic for facility fields.",
                        "GW-59A export uses form template GW-59A.pdf and maps the dedicated GW-59A questionnaire fields on the groundwater record (questions, detail notes, and export-time signer/date).",
                        "GW-59A unanswered questions are exported as blank yes/no boxes (no forced default).",
                        "GW-59 operation type checkboxes are sourced from FacilityPermit.GwOperationLagoon and FacilityPermit.GwOperationSprayField, resolved by facility + sample date.",
                        "GW-59 Sampling Information well-static slots (well depth, diameter, screened interval, measuring point, relative M.P. elevation) come from MonitoringWell for the well selected on the GWMonit record.",
                        "GW-59 per-sample slots (date, depth to water level, volume pumped, metals Y/N) come from GWMonit; metals Y/N export blank when unset.",
                        "GW-59 Date sample collected exports from GWMonit.SampleDate; Date sample analyzed exports from GWMonit.LabSampleAnalyzedDate (Laboratory Information block).",
                        "GW-59 field pH comes from GWMonit.PH. Laboratory Information named rows (COD, coliform, TDS, lab pH, TOC, metals, etc.) are drawn from GWMonitTemplateValue snapshots via Gw59PdfCalibration (Utilities/Gw59PdfCalibration.cs), which maps each PCS code to fixed PDF coordinates on the letter-size template.",
                        "GW-59 Other section accepts up to 10 remaining GW-59 PCS rows with values that do not have a named PDF slot (excluding VOC 78732, water level 82546, and recoverable parameters), in permit sort order. Slots #1–#5 export in the left column (x≈495, width≈83) and #6–#10 in the right column (x≈582.5, width≈81) on the letter-size template; text is clipped to each column box using format: Compound, concentration units.",
                        "VOC merge reads Azure blobs into memory before PdfSharpCore import so valid PDFs no longer fail on non-seekable Azure streams.",
                        "Exports fail with actionable messages when template files or required source data are missing."
                    }
                },
                new ProjectStepFlowViewModel
                {
                    Name = "Operator Logs",
                    Steps =
                    {
                        "Daily/period operational observations are captured.",
                        "Operator Logs are the single source of truth for ORC On Site, Storage Lagoon Freeboard (ft), ORC Arrival Time, and ORC Time on Site (hours).",
                        "WWChar and NDAR edit experiences act as proxies that read/write those date-level values through Operator Logs.",
                        "On successful create/update, MonthlyReportProvisionerService ensures NDAR-1 (create or refresh) for the log month/year.",
                        "NDMR (IrrRprt) auto-create is best-effort: skipped until at least one application log exists for that month (same rule as manual Generate NDMR).",
                        "Deletes refresh NDAR-1 only when a report already exists; they do not auto-create new report records.",
                        "These records support operational traceability, cross-module consistency, and analytics."
                    }
                },
                new ProjectStepFlowViewModel
                {
                    Name = "Monthly Report Auto-Provisioning",
                    Steps =
                    {
                        "MonthlyReportProvisionerService (Services/Implementations/MonthlyReportProvisionerService.cs) runs after operational saves commit, outside the original save transaction.",
                        "Application Log (MonthlyApplication) create/update: ensures NDAR-1 for that month (generate + create if missing, else refresh computed fields) and NDMLR (bare identity row if missing).",
                        "Operator Log create/update: ensures NDAR-1 (create or refresh). NDMR/IrrRprt is created only when irrigation records exist for that month.",
                        "WWChar create/update: ensures NDMR for WWChar month/year after operator-log proxy, WWChar save, and template values are persisted.",
                        "NDAR-1 grid row edit (reverse-write into Operator Logs + Monthly Applications): ensures NDAR-1, NDMR, and NDMLR for the edited row month when refresh is not skipped.",
                        "Manual Reports > Generate actions remain available; duplicate month/facility requests reuse the existing record instead of overwriting curated NDAR-1 data.",
                        "GW-59 has no monthly report entity: each GWMonit record appears in Reports > Groundwater Quality Reports when saved (no separate auto-create step)."
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
                        new List<string> { "Maximum Hourly Loading", "MaxHourlyLoading(in/hr) = Sprayfield Permitted (Max) Hourly Rate", "Static source from sprayfield setup." },
                        new List<string> { "Daily Loading", "DailyLoading(in) = MaxHourlyLoading(in/hr) * (TimeIrrigatedMinutes / 60)", "Time-based operational formula." },
                        new List<string> { "Monthly App Volume", "Volume(gal) = DailyLoading(in) * Area(acres) * 27,154", "Used in monthly application create/edit flow." },
                        new List<string> { "NDAR Daily Loading", "DailyLoading(in) = Volume(gal) / (Area(acres) * 27,154)", "Derived for NDAR displays/totals from stored volume." },
                        new List<string> { "Monthly Loading", "Sum of daily loading values", "Per field." },
                        new List<string> { "12-Month Floating Total", "Current month monthly loading + previous 11 months monthly loading", "Per field." }
                    }
                },
                new ProjectTableViewModel
                {
                    Caption = "GW-59 Facility Information Field Mapping (PDF + Preview)",
                    Headers = { "Form Label", "Primary Source", "Fallback / Notes" },
                    Rows =
                    {
                        new List<string> { "Facility Name", "Facility.Name", "Blank if facility missing." },
                        new List<string> { "Permit Number", "FacilityPermit.PermitNumber (resolved by facility + sample date)", "Facility.PermitNumber when no resolved permit." },
                        new List<string> { "Permittee", "Facility.Permittee", "—" },
                        new List<string> { "Address / City / State / Zip / County", "FacilityPermit fields (preferred) with Facility fallback", "Via Gw59FacilityFieldResolver." },
                        new List<string> { "Permit Expiration Date", "FacilityPermit.EffectiveEndDate", "Facility.PermitExpirationDate when permit end date unavailable." },
                        new List<string> { "Contact Person", "Facility.FacilityContactPerson", "System Admin > Facilities." },
                        new List<string> { "Telephone", "Facility.FacilityContactPersonPhone", "System Admin > Facilities." },
                        new List<string> { "Well Location / Site Name", "MonitoringWell.LocationDescription", "Well selected on GWMonit; explicit load if navigation property is null." },
                        new List<string> { "No. of wells to be sampled", "FacilityPermit.TotalNumberOfSprayfields", "Monitoring-well count fallback when permit sprayfield count unset." },
                        new List<string> { "Lab Name / Certification No.", "CompanyLabOption (record LabOptionId on WWChar/GWMonit)", "Each monitoring record selects its lab; exports resolve from that record (single active company lab used when unset)." }
                    }
                },
                new ProjectTableViewModel
                {
                    Caption = "Monthly Report Auto-Provision Triggers",
                    Headers = { "Operational save", "Month key", "Auto-created reports", "If report already exists" },
                    Rows =
                    {
                        new List<string> { "MonthlyApplication (create/update)", "ApplicationDate month/year", "NDAR-1 + NDMLR", "NDAR-1 refreshed; NDMLR unchanged" },
                        new List<string> { "OperatorLog (create/update)", "LogDate month/year", "NDAR-1 (+ NDMR when applications exist)", "NDAR-1 refreshed; NDMR unchanged or deferred until application log exists" },
                        new List<string> { "WWChar (create/update)", "WWChar Month/Year", "NDMR (IrrRprt)", "No change" },
                        new List<string> { "NDAR-1 grid row edit", "Edited row date month/year", "NDAR-1 + NDMR + NDMLR", "NDAR-1 refreshed; NDMR/NDMLR unchanged if present" },
                        new List<string> { "OperatorLog / MonthlyApplication delete", "Affected month/year", "—", "NDAR-1 refreshed only if report exists; no auto-create" },
                        new List<string> { "GWMonit save", "SampleDate month", "—", "Record listed under Groundwater Quality Reports (no monthly report entity)" }
                    }
                },
                new ProjectTableViewModel
                {
                    Caption = "Report Source Mapping",
                    Headers = { "Report", "Primary Source Models", "Header/Permit Logic", "Fallback Notes" },
                    Rows =
                    {
                        new List<string> { "NDAR1", "NDAR1 + NDAR1Field + NDAR1FieldDaily + MonthlyApplication", "Permit number and county from resolved FacilityPermit for report month (Gw59FacilityFieldResolver)", "Auto-created/refreshed from application/operator logs; grid edit can reverse-write operational rows; export layout includes facility/field checkboxes and footer columns through V." },
                        new List<string> { "NDMR", "WWChar + GWMonit + OperatorLog + permit template PCS rows", "Permit number/version and county from FacilityPermit; certification page uses WWChar LabOptionId + SecondaryLabOptionId and SamplingPerson1/2", "IrrRprt identity auto-created from operator logs or WWChar; export still aggregates live operational data; first two daily columns use canonical Operator Logs; PDF compliance uses X-only checkbox marks with no signature dates." },
                        new List<string> { "NDMLR (annual)", "NDAR-1 + GWMonit + sprayfield volumes", "Permit number and county from resolved FacilityPermit for report period", "Identity row auto-created from application logs (window end = application month/year); export math runs at export time." },
                        new List<string> { "Irrigation Report", "Monthly applications + supporting operational context", "Facility metadata", "Compliance status and summary metrics derive from source entries." },
                        new List<string> { "GW report outputs", "GWMonit + GWMonitTemplateValue + FacilityPermit + CompanyLabOption + MonitoringWell + Facility", "Permit version resolved by facility default selection or sample date; address/county from permit; operation type checkboxes from permit flags; facility block uses Gw59FacilityFieldResolver", "One combined export path produces GW-59 + optional GW-59A + optional VOC PDF. Lab from GWMonit.LabOptionId (CompanyLabOption); no. of wells from permit sprayfield count with monitoring-well fallback." },
                        new List<string> { "ORC/Storage day values", "OperatorLog (canonical) + WWChar/NDAR proxies", "Resolved by facility + exact date", "No duplicate storage in WWChar/NDAR legacy columns." }
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
                    Id = "trace-report-auto-provision",
                    KeywordOrProperty = "Report auto-create / auto-provision",
                    Aliases = { "MonthlyReportProvisioner", "EnsureNdar1", "auto-created report", "bidirectional reports" },
                    Entity = "MonthlyReportProvisionerService",
                    StorageField = "N/A (orchestration service)",
                    UsedInModule = "Operational Data + Reports",
                    FormulaOrTransformation = "Idempotent ensure: NDAR-1 via GenerateMonthlyReportAsync + CreateAsync or RefreshExistingReportForMonthAsync (365-day floating totals use hydraulic volume only, not WWChar PAN); NDMR via IrrRprtService when irrigation records exist; NDMLR via bare NDMLR insert",
                    ReportOutput = "NDAR-1, NDMR (IrrRprt), and NDMLR monthly/annual identity records in Reports lists",
                    FallbackOrValidation = "Duplicate month/facility races are caught; manual Generate reuses existing records; NDAR-1 refresh never overwrites curated dynamic fields on existing reports; NDMR auto-create is skipped when no application logs exist for the month.",
                    Reference = new TraceReferenceViewModel
                    {
                        Label = "Monthly Report Auto-Provisioning",
                        Location = "Services/Implementations/MonthlyReportProvisionerService.cs + Services/Implementations/NDAR1Service.EnsureAndRefreshForMonthAsync"
                    },
                    UsedByReports = { "NDAR1", "NDMR", "NDMLR (annual)" },
                    Steps =
                    {
                        new TraceStepViewModel { Order = 1, Label = "Trigger", Detail = "Application log, operator log, WWChar save, or NDAR-1 grid row edit completes successfully." },
                        new TraceStepViewModel { Order = 2, Label = "Provision", Detail = "MonthlyReportProvisionerService checks facility/month/year and creates missing report records using the same paths as manual Generate." },
                        new TraceStepViewModel { Order = 3, Label = "NDAR-1 sync", Detail = "If NDAR-1 already exists, computed snapshot fields refresh from operational data without replacing curated grid values." },
                        new TraceStepViewModel { Order = 4, Label = "Surface", Detail = "Reports module lists show new records; operational save messages may note NDAR-1 auto-created or refreshed." }
                    },
                    Usages =
                    {
                        new TraceUsageViewModel { Module = "Operational Data", Report = "NDAR1", Destination = "Auto-create or refresh after application/operator log saves." },
                        new TraceUsageViewModel { Module = "Operational Data", Report = "NDMR", Destination = "Auto-create IrrRprt after operator log or WWChar save when irrigation records exist for the month." },
                        new TraceUsageViewModel { Module = "Operational Data", Report = "NDMLR (annual)", Destination = "Auto-create identity after application log save." },
                        new TraceUsageViewModel { Module = "Reports", Report = "NDAR1", Destination = "Grid row edit reverse-write also provisions all three monthly report types." }
                    },
                    FallbackRules =
                    {
                        new TraceFallbackRuleViewModel { Condition = "Report already exists for month", Behavior = "NDAR-1 refreshes; NDMR/NDMLR left unchanged." },
                        new TraceFallbackRuleViewModel { Condition = "No application logs for month", Behavior = "NDMR auto-create skipped; NDAR-1 still created/refreshed from operator log weather data." },
                        new TraceFallbackRuleViewModel { Condition = "Delete operational record", Behavior = "NDAR-1 refreshes if present; no auto-create on delete." }
                    }
                },
                new TraceabilityItemViewModel
                {
                    Id = "trace-daily-loading-ndar",
                    KeywordOrProperty = "DailyLoading",
                    Aliases = { "Daily Loading", "in/ac", "NDAR daily loading" },
                    Entity = "NDAR1FieldDaily",
                    StorageField = "DailyLoading",
                    UsedInModule = "Reports > NDAR1",
                    FormulaOrTransformation = "DailyLoading(in) = Volume(gal) / (Area(acres) * 27,154)",
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
                            Expression = "DailyLoading(in) = Volume(gal) / (Area(acres) * 27,154)",
                            Notes = "NDAR conversion constant."
                        },
                        new TraceFormulaViewModel
                        {
                            Name = "Max Hourly source",
                            Expression = "MaxHourly(in/hr) = Sprayfield Permitted (Max) Hourly Rate",
                            Notes = "Static source; not a duration branch formula."
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
                    FallbackOrValidation = "Save blocked when sprayfield area missing/zero; permitted max hourly rate required for max hourly assignment.",
                    Reference = new TraceReferenceViewModel
                    {
                        Label = "Monthly Application Create/Edit",
                        Location = "Controllers/OperationalDataController.cs"
                    },
                    UsedByReports = { "NDAR1", "Irrigation" },
                    Steps =
                    {
                        new TraceStepViewModel { Order = 1, Label = "Input Source", Detail = "User enters Time Irrigated in Monthly Application form." },
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
                        new TraceFallbackRuleViewModel { Condition = "Permitted max hourly rate missing", Behavior = "Save blocked; configure sprayfield permitted max hourly rate first." }
                    }
                },
                new TraceabilityItemViewModel
                {
                    Id = "trace-orc-storage-canonical",
                    KeywordOrProperty = "ORC On Site / Storage Lagoon Freeboard / ORC Arrival Time / ORC Time on Site",
                    Aliases = { "ORC", "Lagoon Freeboard", "Storage Lagoon Freeboard (ft)", "Arrival Time", "Time On Site" },
                    Entity = "OperatorLog",
                    StorageField = "ORCOnSite + StorageFt + ArrivalTime + TimeOnSiteHours",
                    UsedInModule = "Operational Data > Operator Logs / WWChar / NDAR",
                    FormulaOrTransformation = "Date-level canonical lookup by facility + date; WWChar/NDAR writes are proxied into OperatorLog records.",
                    ReportOutput = "WWChar daily display and NDAR day-level storage surfaces",
                    FallbackOrValidation = "If no OperatorLog exists for a date during proxy write, SAM creates one and stores the values.",
                    Reference = new TraceReferenceViewModel
                    {
                        Label = "Canonical ORC/Storage Flow",
                        Location = "Controllers/OperationalDataController.cs + Services/Implementations/NDAR1RowEditService.cs"
                    },
                    UsedByReports = { "WWChar", "NDAR1", "NDMR" },
                    Steps =
                    {
                        new TraceStepViewModel { Order = 1, Label = "Input Source", Detail = "User can update values directly in Operator Logs or via WWChar/NDAR proxy edit surfaces." },
                        new TraceStepViewModel { Order = 2, Label = "Storage", Detail = "Canonical values persist only on OperatorLog.ORCOnSite, OperatorLog.StorageFt, OperatorLog.ArrivalTime, and OperatorLog.TimeOnSiteHours." },
                        new TraceStepViewModel { Order = 3, Label = "Resolution", Detail = "Readers resolve values per exact facility/date from canonical OperatorLog data." },
                        new TraceStepViewModel { Order = 4, Label = "Surface", Detail = "Changes appear across WWChar, NDAR, NDMR exports, and operator log screens for the same date." }
                    },
                    Usages =
                    {
                        new TraceUsageViewModel { Module = "Operational Data", Report = "WWChar", Destination = "Attachment A daily ORC/storage/arrival/time-on-site columns." },
                        new TraceUsageViewModel { Module = "Reports", Report = "NDAR1", Destination = "Day-row storage views/exports via canonical lookup." },
                        new TraceUsageViewModel { Module = "Reports", Report = "NDMR", Destination = "First two day columns export ORC arrival time and time on site from canonical Operator Logs." }
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
                    ReportOutput = "Compliance checks and NDMR/NDMLR-related output context",
                    FallbackOrValidation = "Monthly Application compliance requires WWChar chemistry including TKN for same month/year.",
                    Reference = new TraceReferenceViewModel
                    {
                        Label = "TKN Validation + Monitoring",
                        Location = "Controllers/OperationalDataController.cs + ApplicationComplianceService"
                    },
                    UsedByReports = { "NDMR", "NDMLR (annual)", "NDAR1 (compliance dependency)" },
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
                },
                new TraceabilityItemViewModel
                {
                    Id = "trace-gw59-facility-info",
                    KeywordOrProperty = "GW-59 Facility Information",
                    Aliases = { "Contact Person", "FacilityContactPerson", "FacilityContactPersonPhone", "CompanyLabOption", "Well Location", "NumberOfWellsToBeSampled", "Gw59FacilityFieldResolver" },
                    Entity = "Facility + FacilityPermit + GWMonit.LabOption + MonitoringWell",
                    StorageField = "Facility.FacilityContactPerson, Facility.FacilityContactPersonPhone, FacilityPermit address/county/sprayfields, GWMonit.LabOptionId -> CompanyLabOption name/cert, MonitoringWell.LocationDescription",
                    UsedInModule = "Reports > Groundwater Quality Reports + Operational Data > GW Monit Preview",
                    FormulaOrTransformation = "BuildGw59ExportModelAsync / BuildGW59ReportAsync map facility block through Gw59FacilityFieldResolver; RenderGw59PdfAsync draws values on GW59_Resized.pdf coordinates via Gw59PdfCalibration.",
                    ReportOutput = "GW-59 PDF Facility Information section and GWMonitReport preview card",
                    FallbackOrValidation = "Address/county from permit with facility fallback; lab from GWMonit record LabOptionId (single active company lab when unset); wells count from permit sprayfield count with monitoring-well fallback.",
                    Reference = new TraceReferenceViewModel
                    {
                        Label = "GW-59 Facility Field Resolver",
                        Location = "Utilities/Gw59FacilityFieldResolver.cs + Controllers/ReportsController.cs + Controllers/OperationalDataController.cs"
                    },
                    UsedByReports = { "GW-59", "GW-59A (combined export)" },
                    Steps =
                    {
                        new TraceStepViewModel { Order = 1, Label = "Setup Source", Detail = "Facility contact fields, permit versions (System Admin > Permits tab), company lab options, and monitoring wells are maintained in System Admin; each GWMonit/WWChar selects its lab on create/edit." },
                        new TraceStepViewModel { Order = 2, Label = "Record Context", Detail = "GWMonit ties export to FacilityId, MonitoringWellId, and SampleDate for permit/well resolution." },
                        new TraceStepViewModel { Order = 3, Label = "Resolver", Detail = "Gw59FacilityFieldResolver supplies contact person, telephone, permit address/county, lab option, well location, and wells-to-sample count." },
                        new TraceStepViewModel { Order = 4, Label = "Report Surface", Detail = "Values render in GWMonitReport preview and ExportGW59Report PDF (Contact Person, Telephone, Well Location, No. of wells)." }
                    },
                    Usages =
                    {
                        new TraceUsageViewModel { Module = "System Admin", Report = "GW-59", Destination = "Facility contact fields, Permits tab, Lab Options tab, monitoring-well master setup." },
                        new TraceUsageViewModel { Module = "Reports", Report = "GW-59", Destination = "Facility Information PDF block via ExportGW59Report." },
                        new TraceUsageViewModel { Module = "Operational Data", Report = "GW-59", Destination = "Preview GW-59 facility card via BuildGW59ReportAsync." }
                    },
                    FallbackRules =
                    {
                        new TraceFallbackRuleViewModel { Condition = "Contact Person blank on PDF", Behavior = "Verify Facility Contact Person is populated in System Admin > Facilities." },
                        new TraceFallbackRuleViewModel { Condition = "Telephone blank on PDF", Behavior = "Populate Facility Contact Person Phone # on the facility record." },
                        new TraceFallbackRuleViewModel { Condition = "Well Location blank", Behavior = "Enter Location Description on the selected monitoring well in System Admin > Monitoring Wells." },
                        new TraceFallbackRuleViewModel { Condition = "No. of wells blank or zero", Behavior = "Set Total Number of Sprayfields on the facility permit, or add monitoring wells as fallback count." }
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
            ["FacilityPermit"] = "System Administration > Permits",
            ["CompanyLabOption"] = "System Administration > Lab Options",
            ["FacilityPermitTemplateParameter"] = "System Administration > Permit Template",
            ["MonthlyApplication"] = "Operational Data > Monthly Applications",
            ["WWChar"] = "Operational Data > WWChar",
            ["WWCharTemplateValue"] = "Operational Data > WWChar (Template Values)",
            ["GWMonit"] = "Operational Data > GW Monitoring",
            ["GWMonitTemplateValue"] = "Operational Data > GW Monitoring (Template Values)",
            ["OperatorLog"] = "Operational Data > Operator Logs",
            ["IrrRprt"] = "Reports > NDMR",
            ["NDAR1"] = "Reports > NDAR1",
            ["NDMLR"] = "Reports > NDMLR",
            ["NDAR1Field"] = "Reports > NDAR1",
            ["NDAR1FieldDaily"] = "Reports > NDAR1"
        };

        var reportHints = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["MonthlyApplication"] = new() { "NDAR1", "NDMLR (annual)", "Irrigation" },
            ["OperatorLog"] = new() { "NDAR1", "NDMR" },
            ["NDAR1"] = new() { "NDAR1" },
            ["NDMLR"] = new() { "NDMLR (annual)" },
            ["IrrRprt"] = new() { "NDMR" },
            ["NDAR1Field"] = new() { "NDAR1" },
            ["NDAR1FieldDaily"] = new() { "NDAR1" },
            ["WWChar"] = new() { "NDMR" },
            ["WWCharTemplateValue"] = new() { "NDMR" },
            ["GWMonit"] = new() { "NDMR", "NDMLR (annual)" },
            ["GWMonitTemplateValue"] = new() { "NDMR", "NDMLR (annual)" },
            ["FacilityPermit"] = new() { "NDMR", "NDAR1" },
            ["FacilityPermitTemplateParameter"] = new() { "NDMR" },
            ["Facility"] = new() { "NDAR1", "NDMR", "Irrigation", "NDMLR (annual)" },
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
                        new List<string> { "Report missing in Reports list after operational save", "NDAR-1 auto-create may have failed (check logs); NDMR is deferred until application logs exist for that month", "Confirm save succeeded; for operator-log-only months expect NDAR-1 but not NDMR; retry save or use Reports > Generate." },
                        new List<string> { "Duplicate report on manual Generate", "System reuses existing NDAR-1/NDMLR or IrrRprt for facility/month/year", "Expected idempotent behavior; operational auto-create uses the same uniqueness rules." },
                        new List<string> { "Permit unarchive causes overlap", "Unarchive blocked with message", "Adjust date ranges or archive conflicting active permit." },
                        new List<string> { "Duplicate permit number+version on create", "Create either restores soft-deleted match or shows friendly duplicate message", "Use archived section or update existing record." },
                        new List<string> { "PCS import duplicate codes", "Importer deduplicates and upserts safely", "Re-run import; duplicates in TSV no longer break process." },
                        new List<string> { "GW-59 Facility Information field blank on PDF", "Export still succeeds; affected slot is left empty", "Contact Person: set Facility ORC Name. Telephone: set Facility/Permit/Operator phone. Well Location: set monitoring-well Location Description. No. of wells: add company monitoring wells in System Admin." },
                        new List<string> { "GW-59 well depth/diameter/screen/measuring point blank", "Export still succeeds; affected slot is left empty", "Set values on MonitoringWell in System Admin > Monitoring Wells, or edit them from GWMonit create/edit Well Data (GW-59) section." },
                        new List<string> { "GW-59 metals yes/no boxes blank on PDF", "Export still succeeds; both yes and no boxes left empty for that question", "Set Metals Samples Collected Unfiltered and/or Metal Samples Field Acidified on the GWMonit record (optional fields; unset is intentional)." }
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
                        "Confirm required operational records exist for month/year (saving data may have already auto-created NDAR-1, NDMR, or NDMLR).",
                        "Check Reports lists before using manual Generate — duplicate Generate reuses the existing record.",
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
                        "Changing facility ORC Name, phone fields, or monitoring-well location descriptions impacts GW-59 Facility Information on export and preview.",
                        "Changing monitoring-well GW-59 fields (depth, diameter, screened interval, measuring point, relative M.P. elevation) impacts GW-59 Sampling Information on export and preview for all samples tied to that well.",
                        "Changing monthly application values impacts compliance projections and downstream reporting.",
                        "Saving application logs, operator logs, or WWChar can auto-create monthly report records (NDAR-1, NDMR, NDMLR) without a separate Generate step.",
                        "NDAR-1 grid edits reverse-write operational data and also run monthly report auto-provisioning for the edited month."
                    }
                }
            }
        };
    }
}
