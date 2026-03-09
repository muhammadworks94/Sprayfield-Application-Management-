using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

public class ApplicationZoneService : IApplicationZoneService
{
    private const decimal PercentTolerance = 0.01m;
    private readonly ApplicationDbContext _context;

    public ApplicationZoneService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ApplicationZone>> GetBySprayfieldIdAsync(Guid sprayfieldId)
    {
        return await _context.ApplicationZones
            .Where(z => z.SprayfieldId == sprayfieldId)
            .OrderBy(z => z.ZoneName)
            .ToListAsync();
    }

    public async Task<ApplicationZone?> GetByIdAsync(Guid id)
    {
        return await _context.ApplicationZones.FirstOrDefaultAsync(z => z.Id == id);
    }

    public async Task<ApplicationZone> CreateAsync(ApplicationZone zone)
    {
        await RecalculateZoneAcresAsync(zone);
        _context.ApplicationZones.Add(zone);
        await _context.SaveChangesAsync();
        return zone;
    }

    public async Task<ApplicationZone> UpdateAsync(ApplicationZone zone)
    {
        var existing = await _context.ApplicationZones.FirstOrDefaultAsync(z => z.Id == zone.Id)
            ?? throw new EntityNotFoundException(nameof(ApplicationZone), zone.Id);

        existing.ZoneName = zone.ZoneName;
        existing.PercentOfField = zone.PercentOfField;
        existing.SoilId = zone.SoilId;
        existing.NozzleId = zone.NozzleId;
        existing.CropId = zone.CropId;
        existing.Active = zone.Active;

        await RecalculateZoneAcresAsync(existing);
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var zone = await _context.ApplicationZones.FirstOrDefaultAsync(z => z.Id == id)
            ?? throw new EntityNotFoundException(nameof(ApplicationZone), id);

        _context.ApplicationZones.Remove(zone);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task ValidatePercentTotalAsync(Guid sprayfieldId, Guid? excludeZoneId = null)
    {
        var query = _context.ApplicationZones.Where(z => z.SprayfieldId == sprayfieldId && z.Active);
        if (excludeZoneId.HasValue)
        {
            query = query.Where(z => z.Id != excludeZoneId.Value);
        }

        var total = await query.SumAsync(z => z.PercentOfField);
        if (Math.Abs(total - 100m) > PercentTolerance)
        {
            throw new BusinessRuleException($"Active application zones must total 100%. Current total: {total:F2}%.");
        }
    }

    public async Task RecalculateForSprayfieldAsync(Guid sprayfieldId)
    {
        var zones = await _context.ApplicationZones
            .Where(z => z.SprayfieldId == sprayfieldId)
            .ToListAsync();

        foreach (var zone in zones)
        {
            await RecalculateZoneAcresAsync(zone);
        }

        await _context.SaveChangesAsync();
    }

    private async Task RecalculateZoneAcresAsync(ApplicationZone zone)
    {
        var field = await _context.Sprayfields.FirstOrDefaultAsync(s => s.Id == zone.SprayfieldId)
            ?? throw new EntityNotFoundException(nameof(Sprayfield), zone.SprayfieldId);

        var acres = field.AcresTotal ?? field.SizeAcres;
        zone.Acres = acres * (zone.PercentOfField / 100m);
    }
}
