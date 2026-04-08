namespace Domain_Layer.Models;

public class Alert
{
    public string Id { get; set; } = string.Empty;
    public string Source_IP { get; set; } = string.Empty;
    public string Destination_IP { get; set; } = string.Empty;
    public string Severity { get; set; }  = string.Empty;
    public string Message { get; set; }  = string.Empty;
    public DateTime Timestamp { get; set; }
}