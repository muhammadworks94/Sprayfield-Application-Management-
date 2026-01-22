namespace SAM.Infrastructure.Authorization;

/// <summary>
/// Centralized authorization policy names.
/// </summary>
public static class Policies
{
    /// <summary>
    /// Requires the user to have the 'admin' role.
    /// </summary>
    public const string RequireAdmin = "RequireAdmin";

    /// <summary>
    /// Requires the user to have either 'admin' or 'company_admin' role.
    /// </summary>
    public const string RequireCompanyAdmin = "RequireCompanyAdmin";

    /// <summary>
    /// Requires the user to have the 'operator' role.
    /// </summary>
    public const string RequireOperator = "RequireOperator";

    /// <summary>
    /// Requires the user to have the 'technician' role.
    /// </summary>
    public const string RequireTechnician = "RequireTechnician";

    /// <summary>
    /// Requires the user to have access to the specified company.
    /// </summary>
    public const string RequireCompanyAccess = "RequireCompanyAccess";
}

