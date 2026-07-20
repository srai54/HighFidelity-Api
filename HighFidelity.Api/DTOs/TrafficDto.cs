namespace HighFidelity.Api.DTOs;

public record TrafficDto(
    int Id,
    string Source,
    double Percentage,
    string SegmentColorHex
);

public record DeleteResponse(int Deleted);
