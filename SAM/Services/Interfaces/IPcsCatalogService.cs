namespace SAM.Services.Interfaces;

public interface IPcsCatalogService
{
    Task<int> ImportFromTsvAsync(string absolutePath, CancellationToken cancellationToken = default);
}

