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

    // ─────────────────────────────────────────────
    // CONNECTION CHECK
    // ─────────────────────────────────────────────
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

    // ─────────────────────────────────────────────
    // GET ALERTS (TIME RANGE SUPPORTED)
    // ─────────────────────────────────────────────
    public async Task<List<Alert>> GetAllAlertsFromElkAsync(TimeSpan? timeRange = null)
    {
        try
        {
            var queryObj = timeRange.HasValue
                ? new
                {
                    size = 10000,
                    sort = new[] { new { timestamp = new { order = "desc" } } },
                    query = new
                    {
                        @bool = new
                        {
                            filter = new object[]
                            {
                                new { exists = new { field = "alert" } },
                                new
                                {
                                    range = new
                                    {
                                        timestamp = new
                                        {
                                            gte = $"now-{(int)timeRange.Value.TotalSeconds}s",
                                            lte = "now"
                                        }
                                    }
                                }
                            }
                        }
                    },
                    _source = new[] { "timestamp", "src_ip", "dest_ip", "alert.severity", "alert.signature" }
                }
                : new  // مفيش range → رجّع كل البيانات
                {
                    size = 10000,
                    sort = new[] { new { timestamp = new { order = "desc" } } },
                    query = new
                    {
                        @bool = new
                        {
                            filter = new object[]
                            {
                                new { exists = new { field = "alert" } }
                            }
                        }
                    },
                    _source = new[] { "timestamp", "src_ip", "dest_ip", "alert.severity", "alert.signature" }
                };
            var json = JsonSerializer.Serialize(queryObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{BaseUrl}/{Index}/_search", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError("ELK search failed [{Status}]: {Error}",
                    (int)response.StatusCode, error);

                return new List<Alert>();
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            return ParseAlerts(responseJson);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error fetching alerts");
            return new List<Alert>();
        }
    }

    // ─────────────────────────────────────────────
    // GET STATS (SYNCED WITH SAME TIME RANGE)
    // ─────────────────────────────────────────────
    public async Task<AlertStats> GetAlertStatsAsync(TimeSpan? timeRange = null)
    { 
        try
        {
            var queryObj = timeRange.HasValue
                ? new
                {
                    size = 0,
                    query = new
                    {
                        @bool = new
                        {
                            filter = new object[]
                            {
                                new
                                {
                                    range = new
                                    {
                                        timestamp = new
                                        {
                                            gte = $"now-{(int)timeRange.Value.TotalSeconds}s",
                                            lte = "now"
                                        }
                                    }
                                }
                            }
                        }
                    },
                    aggs = new
                    {
                        by_severity = new { terms = new { field = "alert.severity" } }
                    }
                }
                : new  // مفيش range → إحصائيات كل البيانات
                {
                    size = 0,
                    query = new
                    {
                        @bool = new
                        {
                            filter = new object[]
                            {
                                new { exists = new { field = "alert" } }
                            }
                        }
                    },
                    aggs = new
                    {
                        by_severity = new { terms = new { field = "alert.severity" } }
                    }
                };

            var json     = JsonSerializer.Serialize(queryObj);
            var content  = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{BaseUrl}/{Index}/_search", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError("ELK stats failed [{Status}]: {Error}",
                    (int)response.StatusCode, error);
                return new AlertStats();
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            return ParseStats(responseJson);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching alert stats");
            return new AlertStats();
        }
    }

    // ─────────────────────────────────────────────
    // PARSE ALERTS
    // ─────────────────────────────────────────────
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

            var timestampRaw = source.GetProperty("timestamp").GetString();

            alerts.Add(new Alert
            {
                Id = idProp.GetString(),
                Source_IP = source.GetProperty("src_ip").GetString(),
                Destination_IP = source.GetProperty("dest_ip").GetString(),
                Severity = alertObj.GetProperty("severity").GetInt32().ToString(),
                Message = alertObj.GetProperty("signature").GetString(),

                // 🔥 SAFE timezone handling
                Timestamp = DateTimeOffset.Parse(timestampRaw).UtcDateTime
            });
        }

        return alerts;
    }

    // ─────────────────────────────────────────────
    // PARSE STATS
    // ─────────────────────────────────────────────
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
                 case 2: stats.High     = count; break;
                 case 3: stats.Medium   = count; break;
                 case 4: stats.Low      = count; break;
                 case 5: stats.Info     = count; break;
             }

             stats.Total += count;
         }

         return stats;
    }
}