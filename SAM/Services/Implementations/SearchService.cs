using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Helpers;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for search operations across different entity types.
/// </summary>
public class SearchService : ISearchService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SearchService> _logger;

    public SearchService(ApplicationDbContext context, ILogger<SearchService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Company>> SearchCompaniesAsync(string searchTerm, string[] searchFields, Guid? companyId = null)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Enumerable.Empty<Company>();
        }

        var query = _context.Companies.AsQueryable();

        // Filter by company ID if provided
        if (companyId.HasValue)
        {
            query = query.Where(c => c.Id == companyId.Value);
        }

        // Filter out soft-deleted records
        query = query.Where(c => !c.IsDeleted);

        // Build dynamic search query
        var searchLower = searchTerm.ToLower();
        var searchPredicate = PredicateBuilder.False<Company>();

        foreach (var field in searchFields)
        {
            switch (field.ToLower())
            {
                case "name":
                    searchPredicate = searchPredicate.Or(c => c.Name.ToLower().Contains(searchLower));
                    break;
                case "contactemail":
                case "email":
                    searchPredicate = searchPredicate.Or(c => !string.IsNullOrEmpty(c.ContactEmail) && c.ContactEmail.ToLower().Contains(searchLower));
                    break;
                case "phonenumber":
                case "phone":
                    searchPredicate = searchPredicate.Or(c => !string.IsNullOrEmpty(c.PhoneNumber) && c.PhoneNumber.Contains(searchTerm));
                    break;
                case "website":
                    searchPredicate = searchPredicate.Or(c => !string.IsNullOrEmpty(c.Website) && c.Website.ToLower().Contains(searchLower));
                    break;
                case "description":
                    searchPredicate = searchPredicate.Or(c => !string.IsNullOrEmpty(c.Description) && c.Description.ToLower().Contains(searchLower));
                    break;
                case "taxid":
                    searchPredicate = searchPredicate.Or(c => !string.IsNullOrEmpty(c.TaxId) && c.TaxId.Contains(searchTerm));
                    break;
                case "licensenumber":
                case "license":
                    searchPredicate = searchPredicate.Or(c => !string.IsNullOrEmpty(c.LicenseNumber) && c.LicenseNumber.Contains(searchTerm));
                    break;
            }
        }

        query = query.Where(searchPredicate);

        var results = await query
            .OrderBy(c => c.Name)
            .ToListAsync();

        _logger.LogInformation("Search for companies with term '{SearchTerm}' returned {Count} results", searchTerm, results.Count);
        return results;
    }
}

