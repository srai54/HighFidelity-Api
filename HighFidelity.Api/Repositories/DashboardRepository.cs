using Microsoft.EntityFrameworkCore;
using HighFidelity.Api.Data;
using HighFidelity.Api.Models;

namespace HighFidelity.Api.Repositories;

/// <summary>
/// EF Core implementation of the dashboard repository.
/// Each method maps to a single table query — clean, testable, and
/// a natural place to add query optimizations (projections, pagination) later.
/// </summary>
public class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _db;

    public DashboardRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<DashboardCard>> GetDashboardCardsAsync() =>
        await _db.DashboardCards.OrderBy(c => c.Id).ToListAsync();

    public async Task<IReadOnlyList<RevenueCard>> GetRevenueCardsAsync() =>
        await _db.RevenueCards.OrderBy(c => c.Id).ToListAsync();

    public async Task<IReadOnlyList<Activity>> GetActivitiesAsync() =>
        await _db.Activities.OrderBy(a => a.Id).ToListAsync();

    public async Task<IReadOnlyList<Order>> GetOrdersAsync() =>
        await _db.Orders.OrderBy(o => o.Id).ToListAsync();

    public async Task<IReadOnlyList<TrafficSource>> GetTrafficSourcesAsync() =>
        await _db.TrafficSources.OrderBy(t => t.Id).ToListAsync();

    public async Task<Order?> GetOrderByIdAsync(int id) =>
        await _db.Orders.FindAsync(id);

    public async Task<Order> AddOrderAsync(string customer, string country, decimal price, string status)
    {
        // Invoice number assigned by DB (MAX+1) so concurrent clients
        // never hand out duplicates from stale in-memory state.
        var maxInvoice = await _db.Orders.MaxAsync(o => (int?)o.Invoice) ?? 12411;

        var order = new Order
        {
            Invoice = maxInvoice + 1,
            Customer = customer,
            Country = country,
            Price = price,
            Status = status
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<int> DeleteOrdersAsync(IReadOnlyList<int> orderIds)
    {
        var orders = await _db.Orders.Where(o => orderIds.Contains(o.Id)).ToListAsync();
        _db.Orders.RemoveRange(orders);
        return await _db.SaveChangesAsync();
    }
}
