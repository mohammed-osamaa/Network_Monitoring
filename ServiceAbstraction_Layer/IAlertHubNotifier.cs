namespace ServiceAbstraction_Layer;

public interface IAlertHubNotifier
{
    Task BroadcastAlertsAsync(IEnumerable<object> alerts);
}