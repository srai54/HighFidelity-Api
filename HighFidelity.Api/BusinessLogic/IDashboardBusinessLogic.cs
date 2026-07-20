using HighFidelity.Api.Models;

namespace HighFidelity.Api.BusinessLogic;

/// <summary>
/// Business Logic (BL) interface.
/// This layer owns validation, orchestration, and cross-cutting concerns —
/// the controllers stay thin and delegate here.
/// </summary>
public interface IDashboardBusinessLogic
{
    Task<IReadOnlyList<DashboardCard>> GetDashboardCardsAsync();
    Task<IReadOnlyList<RevenueCard>> GetRevenueCardsAsync();
    Task<IReadOnlyList<Activity>> GetActivitiesAsync();
    Task<IReadOnlyList<Order>> GetOrdersAsync();
    Task<IReadOnlyList<TrafficSource>> GetTrafficSourcesAsync();
    Task<Order> AddOrderAsync(string customer, string country, decimal price, string status);
    Task<(bool Success, int Deleted, string? Error)> DeleteOrdersAsync(IReadOnlyList<int> orderIds);
}
