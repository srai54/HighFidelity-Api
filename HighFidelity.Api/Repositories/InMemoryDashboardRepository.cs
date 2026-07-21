using HighFidelity.Api.Models;

namespace HighFidelity.Api.Repositories;

/// <summary>
/// Hardcoded, in-process stand-in for <see cref="DashboardRepository"/> — no SQL
/// Server, no LocalDB, nothing to seed. This is the "demo-in-memory" branch:
/// same Controllers/BusinessLogic/JWT/Swagger as the main branch, only this one
/// class differs. That's the point — the Repository seam is exactly what makes
/// swapping the data source a one-file change instead of a rewrite.
///
/// Data mirrors database/seed.sql exactly, so the app looks identical whichever
/// branch is running. Icon fields use the same Font Awesome private-use-area
/// codepoints as the SQL NCHAR() literals, just as C# \u escapes. Registered as
/// a Singleton (see Program.cs) so in-memory adds/deletes persist for the life
/// of the process, the same way SQL rows would persist across requests.
/// </summary>
public class InMemoryDashboardRepository : IDashboardRepository
{
    private readonly object _lock = new();
    private readonly List<Order> _orders;
    private int _nextOrderId;
    private int _nextInvoice;

    private static readonly List<DashboardCard> DashboardCards =
    [
        new() { Id = 1, Title = "Wallet Ballance",  Amount = 4567.53m,  AmountDisplay = "$4,567.53",  Icon = "", ThemeColorHex = "#F7284A" },
        new() { Id = 2, Title = "Referral Earning", Amount = 1689.53m,  AmountDisplay = "$1689.53",   Icon = "", ThemeColorHex = "#7C60FA" },
        new() { Id = 3, Title = "Estimate Sales",   Amount = 2851.53m,  AmountDisplay = "$2851.53",   Icon = "", ThemeColorHex = "#2BC155" },
        new() { Id = 4, Title = "Earning",          Amount = 52567.53m, AmountDisplay = "$52,567.53", Icon = "", ThemeColorHex = "#FF5E9D" }
    ];

    private static readonly List<RevenueCard> RevenueCards =
    [
        new() { Id = 1, Title = "Revenue Status", Value = "$432", Subtitle = "Jan 01 - Jan 10", ChartType = "Bar",  BackgroundHex = "#E1F0FF", AccentHex = "#2196F3" },
        new() { Id = 2, Title = "Page View",      Value = "$432", Subtitle = "",                ChartType = "Area", BackgroundHex = "#FFF8E1", AccentHex = "#FFB822" },
        new() { Id = 3, Title = "Bounce Rate",    Value = "$432", Subtitle = "",                ChartType = "Line", BackgroundHex = "#FBE4D7", AccentHex = "#ED5520" },
        new() { Id = 4, Title = "Revenue Status", Value = "$432", Subtitle = "Jan 01 - Jan 10", ChartType = "Bar",  BackgroundHex = "#F0DEFE", AccentHex = "#8214E8" }
    ];

    private static readonly List<Activity> Activities =
    [
        new() { Id = 1, Title = "Task Updated",      Actor = "Nikolai",  Action = "Updated a Task",      Time = "42 Mins Ago", Icon = "", IconColorHex = "#6259CE" },
        new() { Id = 2, Title = "Deal Added",        Actor = "Panshi",   Action = "Updated a Task",      Time = "1 Day Ago",   Icon = "", IconColorHex = "#EC407A" },
        new() { Id = 3, Title = "Published Article", Actor = "Rasel",    Action = "Published a Article", Time = "42 Mins Ago", Icon = "", IconColorHex = "#29B6F6" },
        new() { Id = 4, Title = "Dock Updated",      Actor = "Reshmi",   Action = "Updated a Dock",      Time = "1 Day Ago",   Icon = "", IconColorHex = "#FFB822" },
        new() { Id = 5, Title = "Replyed Comment",   Actor = "Jenathon", Action = "Added a Comment",     Time = "1 Day Ago",   Icon = "", IconColorHex = "#2BC155" }
    ];

    private static readonly List<TrafficSource> TrafficSources =
    [
        new() { Id = 1, Source = "Facebook",      Percentage = 34, SegmentColorHex = "#2196F3" },
        new() { Id = 2, Source = "Youtube",       Percentage = 55, SegmentColorHex = "#FF5722" },
        new() { Id = 3, Source = "Direct Search", Percentage = 11, SegmentColorHex = "#FFC107" }
    ];

    private static readonly (int Invoice, string Customer, string Country, decimal Price, string Status)[] SeedOrders =
    [
        (12386, "Charly Dues",     "Brazil",    299,  "Process"),
        (12386, "Marko",           "Italy",     2642, "Open"),
        (12386, "Deniyel Onak",    "Russia",    981,  "On Hold"),
        (12386, "Belgiri Bastana", "Korea",     369,  "Process"),
        (12386, "Sarti Gnuska",    "Japan",     1240, "Open"),
        (12387, "Amara Okafor",    "Nigeria",   754,  "Open"),
        (12388, "Liam Carter",     "USA",       1899, "Process"),
        (12389, "Sofia Reyes",     "Mexico",    432,  "On Hold"),
        (12390, "Hans Meyer",      "Germany",   3110, "Open"),
        (12391, "Yuki Tanaka",     "Japan",     587,  "Process"),
        (12392, "Priya Sharma",    "India",     1456, "Open"),
        (12393, "Lucas Silva",     "Brazil",    823,  "On Hold"),
        (12394, "Emma Wilson",     "UK",        2075, "Open"),
        (12395, "Omar Haddad",     "Egypt",     640,  "Process"),
        (12396, "Chen Wei",        "China",     1785, "Open"),
        (12397, "Anna Kowalski",   "Poland",    912,  "On Hold"),
        (12398, "Pierre Dubois",   "France",    1330, "Process"),
        (12399, "Elena Petrova",   "Russia",    468,  "Open"),
        (12400, "Marco Rossi",     "Italy",     2210, "Open"),
        (12401, "Kim Min-jun",     "Korea",     795,  "Process"),
        (12402, "Sara Lindqvist",  "Sweden",    1120, "On Hold"),
        (12403, "David Cohen",     "Israel",    356,  "Open"),
        (12404, "Fatima Zahra",    "Morocco",   1670, "Process"),
        (12405, "Jack Thompson",   "Australia", 940,  "Open"),
        (12406, "Isabella Cruz",   "Spain",     2380, "On Hold"),
        (12407, "Noah Brown",      "Canada",    515,  "Open"),
        (12408, "Aisha Bello",     "Ghana",     1245, "Process"),
        (12409, "Mateus Costa",    "Portugal",  860,  "Open"),
        (12410, "Olga Ivanova",    "Ukraine",   1990, "On Hold"),
        (12411, "Tom Becker",      "Austria",   730,  "Process")
    ];

    public InMemoryDashboardRepository()
    {
        _orders = SeedOrders
            .Select((o, i) => new Order { Id = i + 1, Invoice = o.Invoice, Customer = o.Customer, Country = o.Country, Price = o.Price, Status = o.Status })
            .ToList();
        _nextOrderId = _orders.Count + 1;
        _nextInvoice = _orders.Max(o => o.Invoice) + 1;
    }

    public Task<IReadOnlyList<DashboardCard>> GetDashboardCardsAsync() =>
        Task.FromResult<IReadOnlyList<DashboardCard>>(DashboardCards);

    public Task<IReadOnlyList<RevenueCard>> GetRevenueCardsAsync() =>
        Task.FromResult<IReadOnlyList<RevenueCard>>(RevenueCards);

    public Task<IReadOnlyList<Activity>> GetActivitiesAsync() =>
        Task.FromResult<IReadOnlyList<Activity>>(Activities);

    public Task<IReadOnlyList<TrafficSource>> GetTrafficSourcesAsync() =>
        Task.FromResult<IReadOnlyList<TrafficSource>>(TrafficSources);

    public Task<IReadOnlyList<Order>> GetOrdersAsync()
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<Order>>(_orders.ToList());
    }

    public Task<Order?> GetOrderByIdAsync(int id)
    {
        lock (_lock)
            return Task.FromResult(_orders.FirstOrDefault(o => o.Id == id));
    }

    public Task<Order> AddOrderAsync(string customer, string country, decimal price, string status)
    {
        lock (_lock)
        {
            var order = new Order
            {
                Id = _nextOrderId++,
                Invoice = _nextInvoice++,
                Customer = customer,
                Country = country,
                Price = price,
                Status = status
            };
            _orders.Add(order);
            return Task.FromResult(order);
        }
    }

    public Task<int> DeleteOrdersAsync(IReadOnlyList<int> orderIds)
    {
        lock (_lock)
        {
            var removed = _orders.RemoveAll(o => orderIds.Contains(o.Id));
            return Task.FromResult(removed);
        }
    }
}
