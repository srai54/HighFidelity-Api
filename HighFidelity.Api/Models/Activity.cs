namespace HighFidelity.Api.Models;

public class Activity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string IconColorHex { get; set; } = string.Empty;
}
