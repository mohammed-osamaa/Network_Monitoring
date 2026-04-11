using Domain_Layer.Models;

namespace ServiceAbstraction_Layer;

public interface IElkService
{
    Task<List<Alert>> GetAllAlertsFromElkAsync(TimeSpan? timeRange = null); // بديل GetAlertsFromElkAsync
    Task<AlertStats> GetAlertStatsAsync(TimeSpan? timeRange = null);
    Task<bool> CheckConnectionAsync();
}