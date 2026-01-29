using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for search operations across different entity types.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Searches companies by multiple fields.
    /// </summary>
    /// <param name="searchTerm">Search term to match against.</param>
    /// <param name="searchFields">Fields to search in (e.g., "Name", "ContactEmail", "PhoneNumber").</param>
    /// <param name="companyId">Optional company ID to filter by.</param>
    /// <returns>Matching companies.</returns>
    Task<IEnumerable<Company>> SearchCompaniesAsync(string searchTerm, string[] searchFields, Guid? companyId = null);
}

