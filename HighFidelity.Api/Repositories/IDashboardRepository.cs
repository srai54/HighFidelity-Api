using HighFidelity.Api.Models;

namespace HighFidelity.Api.Repositories;

/// <summary>
/// Repository interface — abstracts all data access behind a seam that
/// can be mocked in unit tests or swapped for a different provider.
/// </summary>
public interface IDashboardRepository
{
    Task<IReadOnlyList<DashboardCard>> GetDashboardCardsAsync();
    Task<IReadOnlyList<RevenueCard>> GetRevenueCardsAsync();
    Task<IReadOnlyList<Activity>> GetActivitiesAsync();
    Task<IReadOnlyList<Order>> GetOrdersAsync();
    Task<IReadOnlyList<TrafficSource>> GetTrafficSourcesAsync();
    Task<Order?> GetOrderByIdAsync(int id);
    Task<Order> AddOrderAsync(string customer, string country, decimal price, string status);
    Task<int> DeleteOrdersAsync(IReadOnlyList<int> orderIds);
}
