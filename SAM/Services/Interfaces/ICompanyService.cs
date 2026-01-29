using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for Company entity operations.
/// </summary>
public interface ICompanyService
{
    Task<IEnumerable<Company>> GetAllAsync();
    Task<Company?> GetByIdAsync(Guid id);
    Task<Company> CreateAsync(Company company);
    Task<Company> UpdateAsync(Company company);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<IEnumerable<Company>> SearchAsync(string searchTerm, string[] searchFields);
}


