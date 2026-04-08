// Services_Layer/ElkService.cs
using System.Text;
using System.Text.Json;
using Domain_Layer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceAbstraction_Layer;

namespace Services_Layer;

public class ElkService(HttpClient httpClient, IConfiguration config, ILogger<ElkService> logger)
    : IElkService
{
    private string Index => config["Elk:Index"] ?? "suricata-*";
    private string BaseUrl => config["Elk:BaseUrl"]!;

    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            var response = await httpClient.GetAsync($"{BaseUrl}/_cluster/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }   
    }

    public async Task<List<Alert>> GetAllAlertsFromElkAsync()
    {
        try
        {
            var queryObj = new
            {
                size = 10000,
                query = new
                {
                    exists = new { field = "alert" } 
                },
                _source = new[]
                {
                    "timestamp", "src_ip", "dest_ip", "alert.severity", "alert.signature"
                }
            };

            var json = JsonSerializer.Serialize(queryObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{BaseUrl}/{Index}/_search", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError("ELK search failed [{Status}]: {Error}", (int)response.StatusCode, error);
                return new List<Alert>();
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            return ParseAlerts(responseJson);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error fetching all alerts");
            return new List<Alert>();
        }
    }

    public async Task<AlertStats> GetAlertStatsAsync()
    {
        try
        {
            var queryObj = new
            {
                size = 0,
                query = new
                {
                    range = new
                    {
                        timestamp = new
                        {
                            gte = DateTime.UtcNow.AddHours(-24)
                                      .ToString("yyyy-MM-ddTHH:mm:ssZ")
                        }
                    }
                },
                aggs = new
                {
                    by_severity = new
                    {
                        terms = new { field = "alert.severity" }
                    }
                }
            };

            var json     = JsonSerializer.Serialize(queryObj);
            var content  = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(
                $"{BaseUrl}/{Index}/_search", content);

            if (!response.IsSuccessStatusCode)
                return new AlertStats();

            var responseJson = await response.Content.ReadAsStringAsync();
            return ParseStats(responseJson);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching alert stats");
            return new AlertStats();
        }
    }

    // ─── Parsing Helpers ───────────────────────────────────────────
    private List<Alert> ParseAlerts(string responseJson)
    {
        var alerts = new List<Alert>();

        using var doc = JsonDocument.Parse(responseJson);

        if (!doc.RootElement.TryGetProperty("hits", out var hitsObj)) return alerts;
        if (!hitsObj.TryGetProperty("hits", out var hitsArray)) return alerts;

        foreach (var hit in hitsArray.EnumerateArray())
        {
            if (!hit.TryGetProperty("_id", out var idProp)) continue;
            if (!hit.TryGetProperty("_source", out var source)) continue;
            if (!source.TryGetProperty("alert", out var alertObj)) continue;

            alerts.Add(new Alert
            {
                Id             = idProp.GetString(), 
                Source_IP      = source.GetProperty("src_ip").GetString(),
                Destination_IP = source.GetProperty("dest_ip").GetString(),
                Severity       = alertObj.GetProperty("severity").GetInt32().ToString(),
                Message        = alertObj.GetProperty("signature").GetString(),
                Timestamp      = DateTime.Parse(source.GetProperty("timestamp").GetString())
                    .ToLocalTime() 
            });
        }

        return alerts;
    }

    private AlertStats ParseStats(string responseJson)
    {
        var stats = new AlertStats();

        using var doc = JsonDocument.Parse(responseJson);

        if (!doc.RootElement.TryGetProperty("aggregations", out var aggs)) return stats;
        if (!aggs.TryGetProperty("by_severity", out var bySeverity))       return stats;
        if (!bySeverity.TryGetProperty("buckets", out var buckets))        return stats;

        foreach (var bucket in buckets.EnumerateArray())
        {
            var key   = bucket.GetProperty("key").GetInt32();
            var count = bucket.GetProperty("doc_count").GetInt64();

            switch (key)
            {
                case 1: stats.Critical = count; break;
                case 2: stats.Warning  = count; break;
                case 3: stats.Info     = count; break;
            }

            stats.Total += count;
        }

        return stats;
    }

    // ─── Safe Property Helpers ─────────────────────────────────────

    private static string? GetString(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) ? v.GetString() : null;

    private static DateTime GetDateTime(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return DateTime.UtcNow;
        return v.TryGetDateTime(out var dt) ? dt : DateTime.UtcNow;
    }
}