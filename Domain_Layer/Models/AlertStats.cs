namespace Domain_Layer.Models;

public class AlertStats
{
    public long Critical { get; set; }
    public long High     { get; set; }
    public long Medium   { get; set; }
    public long Low      { get; set; }
    public long Info     { get; set; }
    public long Total    { get; set; }
}