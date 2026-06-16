using Microsoft.AspNetCore.Routing;

namespace SAM.Services.Models;

/// <summary>
/// Represents a single breadcrumb node.
/// </summary>
public sealed class BreadcrumbItem
{
    public required string Text { get; init; }

    public string? Controller { get; init; }

    public string? Action { get; init; }

    public RouteValueDictionary? RouteValues { get; init; }

    public bool IsCurrent { get; init; }

    public bool HasLink => !IsCurrent && !string.IsNullOrWhiteSpace(Controller) && !string.IsNullOrWhiteSpace(Action);
}
