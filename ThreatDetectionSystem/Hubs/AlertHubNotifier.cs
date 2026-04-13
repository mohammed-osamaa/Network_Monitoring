using Microsoft.AspNetCore.SignalR;
using ServiceAbstraction_Layer;

namespace ThreatDetectionSystem.Hubs;

public class AlertHubNotifier : IAlertHubNotifier
{
    private readonly IHubContext<AlertHub> _hub;

    public AlertHubNotifier(IHubContext<AlertHub> hub) => _hub = hub;

    public async Task BroadcastAlertsAsync(IEnumerable<object> alerts)
        => await _hub.Clients.All.SendAsync("ReceiveAlerts", alerts);
}