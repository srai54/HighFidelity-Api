using HighFidelity.Api.Models;
using HighFidelity.Api.Repositories;

namespace HighFidelity.Api.BusinessLogic;

/// <summary>
/// Business Logic layer. Validates inputs and enforces rules.
/// Controllers delegate here — this is where enterprise rules live.
/// </summary>
public class DashboardBusinessLogic : IDashboardBusinessLogic
{
    private readonly IDashboardRepository _repository;

    public DashboardBusinessLogic(IDashboardRepository repository) => _repository = repository;

    public Task<IReadOnlyList<DashboardCard>> GetDashboardCardsAsync() =>
        _repository.GetDashboardCardsAsync();

    public Task<IReadOnlyList<RevenueCard>> GetRevenueCardsAsync() =>
        _repository.GetRevenueCardsAsync();

    public Task<IReadOnlyList<Activity>> GetActivitiesAsync() =>
        _repository.GetActivitiesAsync();

    public Task<IReadOnlyList<Order>> GetOrdersAsync() =>
        _repository.GetOrdersAsync();

    public Task<IReadOnlyList<TrafficSource>> GetTrafficSourcesAsync() =>
        _repository.GetTrafficSourcesAsync();

    public async Task<Order> AddOrderAsync(string customer, string country, decimal price, string status)
    {
        // ── Business validation ──
        if (string.IsNullOrWhiteSpace(customer))
            throw new ArgumentException("Customer is required.");
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required.");
        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero.");
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status is required.");

        // Additional enterprise rules would go here, e.g.:
        // - Check customer credit limit
        // - Validate country is in supported region list
        // - Enforce max order quantity per customer
        // - Log audit trail

        return await _repository.AddOrderAsync(customer, country, price, status);
    }

    public async Task<(bool Success, int Deleted, string? Error)> DeleteOrdersAsync(IReadOnlyList<int> orderIds)
    {
        if (orderIds.Count == 0)
            return (false, 0, "Provide at least one order ID.");

        // Business rules before deletion:
        // - Check order is in a cancellable status
        // - Verify user has delete permission
        // - Archive before hard-delete

        var deleted = await _repository.DeleteOrdersAsync(orderIds);
        return (true, deleted, null);
    }
}
