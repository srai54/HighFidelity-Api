namespace HighFidelity.Api.DTOs;

/// <summary>
/// Create-order request shape. Deliberately not the Order model — Id and
/// Invoice are server-assigned and must never be settable by the client.
/// </summary>
public record NewOrderRequest(
    string Customer,
    string Country,
    decimal Price,
    string Status
);
