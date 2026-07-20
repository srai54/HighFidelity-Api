namespace HighFidelity.Api.Models;

public class DashboardCard
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AmountDisplay { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string ThemeColorHex { get; set; } = string.Empty;
}
