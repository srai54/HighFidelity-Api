namespace HighFidelity.Api.DTOs;

public record LoginResponseDto(string Token, DateTime ExpiresAtUtc);
