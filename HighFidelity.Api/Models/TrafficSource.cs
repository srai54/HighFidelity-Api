namespace HighFidelity.Api.Models;

public class TrafficSource
{
    public int Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public double Percentage { get; set; }
    public string SegmentColorHex { get; set; } = string.Empty;
}
