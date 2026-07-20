using HighFidelity.Api.DTOs;
using HighFidelity.Api.Mappings;
using HighFidelity.Api.Repositories;

namespace HighFidelity.Api.Services;

/// <summary>
/// Business Logic layer. Validates inputs, enforces rules, maps entities to DTOs.
/// Controllers delegate here — this is where enterprise rules live.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _repository;

    public DashboardService(IDashboardRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<DashboardCardDto>> GetDashboardCardsAsync()
    {
        var cards = await _repository.GetDashboardCardsAsync();
        return cards.Select(c => c.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<RevenueCardDto>> GetRevenueCardsAsync()
    {
        var cards = await _repository.GetRevenueCardsAsync();
        return cards.Select(c => c.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<ActivityDto>> GetActivitiesAsync()
    {
        var activities = await _repository.GetActivitiesAsync();
        return activities.Select(a => a.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrdersAsync()
    {
        var orders = await _repository.GetOrdersAsync();
        return orders.Select(o => o.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<TrafficDto>> GetTrafficSourcesAsync()
    {
        var sources = await _repository.GetTrafficSourcesAsync();
        return sources.Select(s => s.ToDto()).ToList();
    }

    public async Task<OrderDto> AddOrderAsync(string customer, string country, decimal price, string status)
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

        var order = await _repository.AddOrderAsync(customer, country, price, status);
        return order.ToDto();
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
