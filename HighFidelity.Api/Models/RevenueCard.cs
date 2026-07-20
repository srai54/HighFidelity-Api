namespace HighFidelity.Api.Models;

public class RevenueCard
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ChartType { get; set; } = string.Empty;
    public string BackgroundHex { get; set; } = string.Empty;
    public string AccentHex { get; set; } = string.Empty;
}
