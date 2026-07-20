namespace HighFidelity.Api.DTOs;

public record RevenueCardDto(
    int Id,
    string Title,
    string Value,
    string? Subtitle,
    string ChartType,
    string BackgroundHex,
    string AccentHex
);
