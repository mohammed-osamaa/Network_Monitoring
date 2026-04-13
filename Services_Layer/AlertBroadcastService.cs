using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceAbstraction_Layer;

namespace Services_Layer;

public class AlertBroadcastService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AlertBroadcastService> _logger;
    private DateTime _lastCheck = DateTime.UtcNow.AddMinutes(-1);

    public AlertBroadcastService(
        IServiceProvider services,
        ILogger<AlertBroadcastService> logger)
    {
        _services = services;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await BroadcastNewAlertsAsync();
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }

    private async Task BroadcastNewAlertsAsync()
    {
        try
        {
            using var scope   = _services.CreateScope();
            var elk           = scope.ServiceProvider.GetRequiredService<IElkService>();
            var hubNotifier   = scope.ServiceProvider.GetRequiredService<IAlertHubNotifier>();

            var alerts = await elk.GetAllAlertsFromElkAsync(
                timeRange: DateTime.UtcNow - _lastCheck);

            var newAlerts = alerts
                .Where(a => a.Timestamp > _lastCheck)
                .OrderBy(a => a.Timestamp)
                .ToList();

            if (!newAlerts.Any()) return;

            _lastCheck = newAlerts.Max(a => a.Timestamp);

            await hubNotifier.BroadcastAlertsAsync(newAlerts);

            _logger.LogInformation("Broadcasted {Count} new alerts", newAlerts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AlertBroadcastService");
        }
    }
}