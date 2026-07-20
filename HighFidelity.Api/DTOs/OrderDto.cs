namespace HighFidelity.Api.DTOs;

public record OrderDto(
    int Id,
    int Invoice,
    string Customer,
    string Country,
    decimal Price,
    string Status
);

public record NewOrderRequest(
    string Customer,
    string Country,
    decimal Price,
    string Status
);
