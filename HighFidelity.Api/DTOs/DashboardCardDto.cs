namespace HighFidelity.Api.DTOs;

public record DashboardCardDto(
    int Id,
    string Title,
    decimal Amount,
    string AmountDisplay,
    string Icon,
    string ThemeColorHex
);
