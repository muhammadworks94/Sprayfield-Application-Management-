using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using SAM.Services.Interfaces;
using SAM.Services.Models;

namespace SAM.Services.Implementations;

public class BreadcrumbService : IBreadcrumbService
{
    private static readonly IReadOnlyDictionary<string, string> SystemAdminTabLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["facilities"] = "Facilities",
        ["soils"] = "Soil Types",
        ["crops"] = "Crops",
        ["nozzles"] = "Nozzles",
        ["sprayfields"] = "Sprayfields",
        ["monitoringwells"] = "Monitoring Wells",
        ["permits"] = "Permits",
        ["laboptions"] = "Lab Options"
    };

    private static readonly IReadOnlyDictionary<string, string> SectionDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Dashboard"] = "Dashboard",
        ["Analytics"] = "Analytics",
        ["OperationalData"] = "Operations",
        ["Reports"] = "Reports",
        ["SystemAdmin"] = "System Administration",
        ["UserManagement"] = "Users",
        ["CompanyManagement"] = "Manage Companies",
        ["UserActivityLogs"] = "User Activity Logs",
        ["ErrorLogs"] = "Error Logs",
        ["SystemSettings"] = "System Settings",
        ["Account"] = "Account",
        ["Home"] = "Home"
    };

    public IReadOnlyList<BreadcrumbItem> Build(ViewContext viewContext)
    {
        var routeValues = viewContext.RouteData.Values;
        var controller = routeValues["controller"]?.ToString() ?? string.Empty;
        var action = routeValues["action"]?.ToString() ?? string.Empty;

        if (string.Equals(controller, "Dashboard", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                new BreadcrumbItem
                {
                    Text = "Dashboard",
                    IsCurrent = true
                }
            };
        }

        var section = ResolveSection(controller, action);
        var currentText = ResolveCurrentText(viewContext, controller, action);

        if (section is not null &&
            string.Equals(section.Text, currentText, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(section.Action, action, StringComparison.OrdinalIgnoreCase))
        {
            currentText = HumanizeAction(action, controller);
        }

        var items = new List<BreadcrumbItem>
        {
            new()
            {
                Text = "Dashboard",
                Controller = "Dashboard",
                Action = "Index"
            }
        };

        if (section is not null)
        {
            if (string.Equals(section.Text, currentText, StringComparison.OrdinalIgnoreCase))
            {
                items.Add(new BreadcrumbItem
                {
                    Text = section.Text,
                    IsCurrent = true
                });
                return items;
            }

            items.Add(section);
        }

        if (!string.Equals(currentText, "Dashboard", StringComparison.OrdinalIgnoreCase))
        {
            items.Add(new BreadcrumbItem
            {
                Text = currentText,
                IsCurrent = true
            });
        }

        return items;
    }

    private static string ResolveCurrentText(ViewContext viewContext, string controller, string action)
    {
        if (string.Equals(controller, "SystemAdmin", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(action, "SystemAdmin", StringComparison.OrdinalIgnoreCase))
        {
            var tab = viewContext.HttpContext.Request.Query["tab"].ToString();
            if (!string.IsNullOrWhiteSpace(tab) && SystemAdminTabLabels.TryGetValue(tab, out var tabLabel))
            {
                return tabLabel;
            }
        }

        var title = viewContext.ViewData["Title"]?.ToString();
        if (!string.IsNullOrWhiteSpace(title))
        {
            return title;
        }

        return HumanizeAction(action, controller);
    }

    private static BreadcrumbItem? ResolveSection(string controller, string action)
    {
        if (string.Equals(controller, "Dashboard", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(controller, "OperationalData", StringComparison.OrdinalIgnoreCase))
        {
            if (action.StartsWith("WWChar", StringComparison.OrdinalIgnoreCase))
            {
                return Link("Wastewater Monitoring", "OperationalData", "WWChars");
            }

            if (action.StartsWith("GWMonit", StringComparison.OrdinalIgnoreCase))
            {
                return Link("Groundwater Monitoring", "OperationalData", "GWMonits");
            }

            if (action.StartsWith("OperatorLog", StringComparison.OrdinalIgnoreCase))
            {
                return Link("Operations", "OperationalData", "OperatorLogs");
            }

            if (action.StartsWith("MonthlyApplication", StringComparison.OrdinalIgnoreCase))
            {
                return Link("Operations", "OperationalData", "MonthlyApplications");
            }
        }

        if (string.Equals(controller, "Reports", StringComparison.OrdinalIgnoreCase))
        {
            if (action.Contains("NDAR1", StringComparison.OrdinalIgnoreCase) ||
                action.Contains("NDMR", StringComparison.OrdinalIgnoreCase) ||
                action.Contains("NDMLR", StringComparison.OrdinalIgnoreCase))
            {
                return Link("Non-Discharge Reports", "Reports", "NDAR1Reports");
            }

            return Link("Irrigation Reports", "Reports", "IrrigationReports");
        }

        if (string.Equals(controller, "SystemAdmin", StringComparison.OrdinalIgnoreCase))
        {
            return Link("System Administration", "SystemAdmin", "SystemAdmin");
        }

        if (string.Equals(controller, "UserManagement", StringComparison.OrdinalIgnoreCase))
        {
            if (action.StartsWith("Admin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(action, "ManageAdmins", StringComparison.OrdinalIgnoreCase))
            {
                return Link("Global Admins", "UserManagement", "ManageAdmins");
            }

            if (action.StartsWith("UserRequest", StringComparison.OrdinalIgnoreCase))
            {
                return Link("User Requests", "UserManagement", "UserRequests");
            }

            return Link("Users", "UserManagement", "Index");
        }

        if (string.Equals(controller, "CompanyManagement", StringComparison.OrdinalIgnoreCase))
        {
            if (action.StartsWith("CompanyRequest", StringComparison.OrdinalIgnoreCase))
            {
                return Link("Company Requests", "CompanyManagement", "CompanyRequests");
            }

            return Link("Manage Companies", "CompanyManagement", "Index");
        }

        if (string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase))
        {
            return Link("Account", "Account", "Profile");
        }

        if (SectionDefaults.TryGetValue(controller, out var sectionLabel))
        {
            return Link(sectionLabel, controller, "Index");
        }

        return Link(Humanize(controller), controller, "Index");
    }

    private static string HumanizeAction(string action, string controller)
    {
        if (string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase))
        {
            return SectionDefaults.TryGetValue(controller, out var sectionName)
                ? sectionName
                : Humanize(controller);
        }

        if (action.EndsWith("Create", StringComparison.OrdinalIgnoreCase))
        {
            return $"Create {Humanize(action[..^"Create".Length])}";
        }

        if (action.EndsWith("Edit", StringComparison.OrdinalIgnoreCase))
        {
            return $"Edit {Humanize(action[..^"Edit".Length])}";
        }

        if (action.EndsWith("Details", StringComparison.OrdinalIgnoreCase))
        {
            return $"{Humanize(action[..^"Details".Length])} Details";
        }

        if (action.EndsWith("Report", StringComparison.OrdinalIgnoreCase) && !action.StartsWith("Generate", StringComparison.OrdinalIgnoreCase))
        {
            return $"{Humanize(action[..^"Report".Length])} Report";
        }

        if (action.StartsWith("Generate", StringComparison.OrdinalIgnoreCase) && action.EndsWith("Report", StringComparison.OrdinalIgnoreCase))
        {
            var reportName = action["Generate".Length..^"Report".Length];
            return $"Generate {Humanize(reportName)} Report";
        }

        if (action.EndsWith("Requests", StringComparison.OrdinalIgnoreCase))
        {
            return Humanize(action);
        }

        if (action.EndsWith("Approve", StringComparison.OrdinalIgnoreCase))
        {
            return $"Approve {Humanize(action[..^"Approve".Length])}";
        }

        return Humanize(action);
    }

    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace('_', ' ').Trim();
        normalized = Regex.Replace(normalized, "([a-z0-9])([A-Z])", "$1 $2");
        normalized = Regex.Replace(normalized, "([A-Za-z])([0-9])", "$1 $2");
        normalized = Regex.Replace(normalized, "([0-9])([A-Za-z])", "$1 $2");
        normalized = Regex.Replace(normalized, @"\s+", " ");

        return normalized switch
        {
            "NDAR 1" => "NDAR-1",
            "GW Monit" => "Groundwater Monitoring",
            "WW Chars" => "Wastewater Characteristics",
            _ => normalized
        };
    }

    private static BreadcrumbItem Link(string text, string controller, string action, RouteValueDictionary? routeValues = null)
    {
        return new BreadcrumbItem
        {
            Text = text,
            Controller = controller,
            Action = action,
            RouteValues = routeValues
        };
    }
}
