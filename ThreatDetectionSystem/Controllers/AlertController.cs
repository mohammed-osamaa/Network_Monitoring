using Domain_Layer.Models;
using Microsoft.AspNetCore.Mvc;
using ServiceAbstraction_Layer;

namespace ThreatDetectionSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertController : ControllerBase
{
   private readonly IElkService _elk;

   public AlertController(IElkService elk) => _elk = elk;

   [HttpGet]
   public async Task<IActionResult> GetAll([FromQuery] string range = "1h")
   {
      var timeRange = range switch
      {
         "30s" => TimeSpan.FromSeconds(30),
         "1m"  => TimeSpan.FromMinutes(1),
         "1h"  => TimeSpan.FromHours(1),
         "24h" => TimeSpan.FromHours(24),
         _     => TimeSpan.FromHours(1)
      };

      var alerts = await _elk.GetAllAlertsFromElkAsync(timeRange);
      return Ok(alerts);
   }

   [HttpGet("stats")]
   public async Task<IActionResult> Stats([FromQuery] string range = "1h")
   {
      var timeRange = range switch
      {
         "30s" => TimeSpan.FromSeconds(30),
         "1m"  => TimeSpan.FromMinutes(1),
         "1h"  => TimeSpan.FromHours(1),
         "24h" => TimeSpan.FromHours(24),
         _     => TimeSpan.FromHours(1)
      };
      var stats = await _elk.GetAlertStatsAsync(timeRange);
      return Ok(stats);
   }

   [HttpGet("health")]
   public async Task<IActionResult> Health()
   {
      var ok = await _elk.CheckConnectionAsync();
      return ok ? Ok("connected") : StatusCode(503, "elasticsearch unreachable");
   }
}