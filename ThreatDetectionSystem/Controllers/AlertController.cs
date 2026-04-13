using Microsoft.AspNetCore.Mvc;
using ServiceAbstraction_Layer;

[ApiController]
[Route("api/[controller]")]
public class AlertController : ControllerBase
{
    private readonly IElkService _elk;

    public AlertController(IElkService elk) => _elk = elk;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? range = null)
    {
        var alerts = await _elk.GetAllAlertsFromElkAsync(ParseRange(range));
        return Ok(alerts);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats([FromQuery] string? range = null)
    {
        var stats = await _elk.GetAlertStatsAsync(ParseRange(range));
        return Ok(stats);
    }
    
    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        var ok = await _elk.CheckConnectionAsync();
        return ok ? Ok("connected") : StatusCode(503, "elasticsearch unreachable");
    }

    private static TimeSpan? ParseRange(string? range) => range switch
    {
        "30s" => TimeSpan.FromSeconds(30),
        "1m"  => TimeSpan.FromMinutes(1),
        "1h"  => TimeSpan.FromHours(1),
        "24h" => TimeSpan.FromHours(24),
        "all" => null,  
        null  => null,  
        _     => null
    };
}