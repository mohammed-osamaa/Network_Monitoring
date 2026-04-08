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
   public async Task<IActionResult> GetAll()
   {
      var alerts = await _elk.GetAllAlertsFromElkAsync();
      return Ok(alerts);
   }

   [HttpGet("stats")]
   public async Task<IActionResult> Stats()
   {
      var stats = await _elk.GetAlertStatsAsync();
      return Ok(stats);
   }

   [HttpGet("health")]
   public async Task<IActionResult> Health()
   {
      var ok = await _elk.CheckConnectionAsync();
      return ok ? Ok("connected") : StatusCode(503, "elasticsearch unreachable");
   }
}