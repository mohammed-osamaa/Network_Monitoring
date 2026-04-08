namespace Domain_Layer.Models;

public class AlertStats
{
    public long Critical { get; set; }
    public long Warning  { get; set; }
    public long Info     { get; set; }
    public long Total    { get; set; }
}