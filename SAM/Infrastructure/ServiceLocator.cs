using Microsoft.Extensions.DependencyInjection;

namespace SAM.Infrastructure;

/// <summary>
/// Service locator pattern for accessing services when DI is not directly available.
/// Should be used sparingly and only when necessary (e.g., in DbContext SaveChanges).
/// </summary>
public static class ServiceLocator
{
    private static IServiceProvider? _serviceProvider;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static T? GetService<T>() where T : class
    {
        return _serviceProvider?.GetService<T>();
    }

    public static T GetRequiredService<T>() where T : class
    {
        return _serviceProvider?.GetRequiredService<T>() 
            ?? throw new InvalidOperationException("Service locator has not been initialized.");
    }
}


