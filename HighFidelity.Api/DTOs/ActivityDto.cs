namespace HighFidelity.Api.DTOs;

public record ActivityDto(
    int Id,
    string Title,
    string Actor,
    string Action,
    string Time,
    string Icon,
    string IconColorHex
);
