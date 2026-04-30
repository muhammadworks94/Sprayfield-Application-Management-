using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

public class PcsCatalogService : IPcsCatalogService
{
    private readonly ApplicationDbContext _context;

    public PcsCatalogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> ImportFromTsvAsync(string absolutePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(absolutePath))
        {
            return 0;
        }

        var lines = await File.ReadAllLinesAsync(absolutePath, cancellationToken);
        var changed = 0;
        var parsedRows = new Dictionary<string, (string UserFriendly, string Official, string Units)>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var cols = line.Split('\t');
            if (cols.Length < 4)
            {
                continue;
            }

            var pcsCode = cols[0].Trim();
            if (string.IsNullOrWhiteSpace(pcsCode))
            {
                continue;
            }

            var userFriendly = cols[1].Trim();
            var official = cols[2].Trim();
            var units = cols[3].Trim();

            // Keep the last occurrence for duplicate PCS rows in the source file.
            parsedRows[pcsCode] = (userFriendly, official, units);
        }

        if (parsedRows.Count == 0)
        {
            return 0;
        }

        var existingRows = await _context.PcsParameterCatalogs
            .Where(x => parsedRows.Keys.Contains(x.PcsCode))
            .ToDictionaryAsync(x => x.PcsCode, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var row in parsedRows)
        {
            var pcsCode = row.Key;
            var userFriendly = row.Value.UserFriendly;
            var official = row.Value.Official;
            var units = row.Value.Units;

            if (!existingRows.TryGetValue(pcsCode, out var existing))
            {
                _context.PcsParameterCatalogs.Add(new PcsParameterCatalog
                {
                    PcsCode = pcsCode,
                    UserFriendlyName = userFriendly,
                    OfficialParameterName = official,
                    AcceptedUnits = units,
                    IsActive = true
                });
                changed++;
                continue;
            }

            if (existing.UserFriendlyName == userFriendly &&
                existing.OfficialParameterName == official &&
                existing.AcceptedUnits == units &&
                existing.IsActive)
            {
                continue;
            }

            existing.UserFriendlyName = userFriendly;
            existing.OfficialParameterName = official;
            existing.AcceptedUnits = units;
            existing.IsActive = true;
            changed++;
        }

        if (changed > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return changed;
    }
}
