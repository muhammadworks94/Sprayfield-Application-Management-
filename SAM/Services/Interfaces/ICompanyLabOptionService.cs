using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

public interface ICompanyLabOptionService
{
    Task<IEnumerable<CompanyLabOption>> GetAllAsync(Guid? companyId = null);
    Task<CompanyLabOption?> GetByIdAsync(Guid id);
    Task<CompanyLabOption> CreateAsync(CompanyLabOption labOption);
    Task UpdateAsync(CompanyLabOption labOption);
    Task DeleteAsync(Guid id);
    Task ReorderAsync(Guid companyId, IReadOnlyList<Guid> orderedIds);
}
