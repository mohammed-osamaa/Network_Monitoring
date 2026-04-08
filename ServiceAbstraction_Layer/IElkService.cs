using Domain_Layer.Models;

namespace ServiceAbstraction_Layer;

public interface IElkService
{
    Task<List<Alert>> GetAllAlertsFromElkAsync(); // بديل GetAlertsFromElkAsync
    Task<AlertStats> GetAlertStatsAsync();
    Task<bool> CheckConnectionAsync();
}