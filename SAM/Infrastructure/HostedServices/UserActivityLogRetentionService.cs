using Microsoft.Extensions.Options;
using SAM.Services.Interfaces;
using SAM.Services.Models;

namespace SAM.Infrastructure.HostedServices;

public class UserActivityLogRetentionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserActivityLogRetentionService> _logger;
    private readonly ActivityLogOptions _options;

    public UserActivityLogRetentionService(
        IServiceScopeFactory scopeFactory,
        IOptions<ActivityLogOptions> options,
        ILogger<UserActivityLogRetentionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = _options.PurgeIntervalHours <= 0 ? 24 : _options.PurgeIntervalHours;
        var retentionDays = _options.RetentionDays <= 0 ? 180 : _options.RetentionDays;

        using var timer = new PeriodicTimer(TimeSpan.FromHours(intervalHours));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IUserActivityLogService>();
                var errorLogService = scope.ServiceProvider.GetRequiredService<IErrorLogService>();
                var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
                var activityPurged = await service.PurgeOlderThanAsync(cutoff, stoppingToken);
                var errorPurged = await errorLogService.PurgeOlderThanAsync(cutoff, stoppingToken);
                _logger.LogInformation(
                    "Log retention run completed. Purged {ActivityCount} user activity logs and {ErrorCount} error logs.",
                    activityPurged,
                    errorPurged);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed while purging user activity logs.");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }
}
