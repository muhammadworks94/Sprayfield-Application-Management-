namespace SAM.Infrastructure.Helpers;

/// <summary>
/// Maps Identity role names to user-facing display labels.
/// </summary>
public static class RoleDisplayHelper
{
    private static readonly string[] RolePriority =
    [
        "admin",
        "company_admin",
        "operator",
        "technician"
    ];

    public static string GetDisplayName(string roleName)
    {
        return roleName switch
        {
            "admin" => "System Administrator",
            "company_admin" => "Company Administrator",
            "operator" => "Operator",
            "technician" => "Technician",
            _ => roleName
        };
    }

    public static string GetPrimaryRoleDisplayName(IEnumerable<string> roleNames)
    {
        var roles = roleNames as IList<string> ?? roleNames.ToList();
        if (roles.Count == 0)
        {
            return "User";
        }

        foreach (var role in RolePriority)
        {
            if (roles.Contains(role, StringComparer.Ordinal))
            {
                return GetDisplayName(role);
            }
        }

        return GetDisplayName(roles[0]);
    }
}
