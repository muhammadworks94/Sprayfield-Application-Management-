using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

public class CompanyLabOptionService : ICompanyLabOptionService
{
    private readonly ApplicationDbContext _context;

    public CompanyLabOptionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CompanyLabOption>> GetAllAsync(Guid? companyId = null)
    {
        var query = _context.CompanyLabOptions
            .AsNoTracking()
            .Include(x => x.Company)
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(x => x.CompanyId == companyId.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<CompanyLabOption?> GetByIdAsync(Guid id) =>
        await _context.CompanyLabOptions
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<CompanyLabOption> CreateAsync(CompanyLabOption labOption)
    {
        if (labOption.SortOrder <= 0)
        {
            var maxSort = await _context.CompanyLabOptions
                .Where(x => x.CompanyId == labOption.CompanyId)
                .Select(x => (int?)x.SortOrder)
                .MaxAsync() ?? 0;
            labOption.SortOrder = maxSort + 1;
        }

        _context.CompanyLabOptions.Add(labOption);
        await _context.SaveChangesAsync();
        return labOption;
    }

    public async Task UpdateAsync(CompanyLabOption labOption)
    {
        var existing = await _context.CompanyLabOptions.FirstOrDefaultAsync(x => x.Id == labOption.Id);
        if (existing == null)
        {
            throw new EntityNotFoundException(nameof(CompanyLabOption), labOption.Id);
        }

        existing.Name = labOption.Name;
        existing.CertificationNumber = labOption.CertificationNumber;
        existing.SortOrder = labOption.SortOrder;
        existing.IsActive = labOption.IsActive;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var existing = await _context.CompanyLabOptions.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null)
        {
            throw new EntityNotFoundException(nameof(CompanyLabOption), id);
        }

        existing.IsDeleted = true;
        existing.IsActive = false;

        var facilitiesUsingDefault = await _context.Facilities
            .Where(f => f.DefaultLabOptionId == id)
            .ToListAsync();
        foreach (var facility in facilitiesUsingDefault)
        {
            facility.DefaultLabOptionId = null;
        }

        await _context.SaveChangesAsync();
    }

    public async Task ReorderAsync(Guid companyId, IReadOnlyList<Guid> orderedIds)
    {
        var options = await _context.CompanyLabOptions
            .Where(x => x.CompanyId == companyId)
            .ToListAsync();

        for (var i = 0; i < orderedIds.Count; i++)
        {
            var option = options.FirstOrDefault(x => x.Id == orderedIds[i]);
            if (option != null)
            {
                option.SortOrder = i + 1;
            }
        }

        await _context.SaveChangesAsync();
    }
}
