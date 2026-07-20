using HighFidelity.Api.DTOs;

namespace HighFidelity.Api.BusinessLogic;

/// <summary>
/// Business Logic (BL) interface.
/// This layer owns validation, orchestration, and cross-cutting concerns —
/// the controllers stay thin and delegate here.
/// </summary>
public interface IDashboardBusinessLogic
{
    Task<IReadOnlyList<DashboardCardDto>> GetDashboardCardsAsync();
    Task<IReadOnlyList<RevenueCardDto>> GetRevenueCardsAsync();
    Task<IReadOnlyList<ActivityDto>> GetActivitiesAsync();
    Task<IReadOnlyList<OrderDto>> GetOrdersAsync();
    Task<IReadOnlyList<TrafficDto>> GetTrafficSourcesAsync();
    Task<OrderDto> AddOrderAsync(string customer, string country, decimal price, string status);
    Task<(bool Success, int Deleted, string? Error)> DeleteOrdersAsync(IReadOnlyList<int> orderIds);
}
