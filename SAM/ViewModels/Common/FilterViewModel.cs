using Microsoft.AspNetCore.Mvc.Rendering;

namespace SAM.ViewModels.Common;

/// <summary>
/// ViewModel for configuring dynamic filters on list pages.
/// </summary>
public class FilterViewModel
{
    /// <summary>
    /// Name of the page/entity type (e.g., "Companies", "Facilities").
    /// </summary>
    public string PageName { get; set; } = string.Empty;

    /// <summary>
    /// List of filter fields to display.
    /// </summary>
    public List<FilterField> Fields { get; set; } = new();

    /// <summary>
    /// Placeholder text for the search input.
    /// </summary>
    public string SearchPlaceholder { get; set; } = "Search...";

    /// <summary>
    /// Whether to enable text search functionality.
    /// </summary>
    public bool EnableSearch { get; set; }

    /// <summary>
    /// Current search term value.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Action name for form submission (defaults to current action).
    /// </summary>
    public string? ActionName { get; set; }

    /// <summary>
    /// Controller name for form submission (defaults to current controller).
    /// </summary>
    public string? ControllerName { get; set; }
}

/// <summary>
/// Represents a single filter field configuration.
/// </summary>
public class FilterField
{
    /// <summary>
    /// Query parameter name (e.g., "companyId", "facilityId").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display label for the filter field.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Type of filter field (Dropdown, Text, Date, etc.).
    /// </summary>
    public FilterFieldType Type { get; set; }

    /// <summary>
    /// Options for dropdown fields.
    /// </summary>
    public SelectList? Options { get; set; }

    /// <summary>
    /// Current selected/value for the field.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// CSS column class (e.g., "col-md-3", "col-md-4").
    /// </summary>
    public string ColumnClass { get; set; } = "col-md-3";

    /// <summary>
    /// Icon class for the field (e.g., "bi bi-building").
    /// </summary>
    public string? IconClass { get; set; }

    /// <summary>
    /// Whether the field should submit the form on change (for dropdowns).
    /// </summary>
    public bool SubmitOnChange { get; set; } = true;
}

/// <summary>
/// Types of filter fields supported.
/// </summary>
public enum FilterFieldType
{
    Dropdown,
    Text,
    Date,
    Number
}

