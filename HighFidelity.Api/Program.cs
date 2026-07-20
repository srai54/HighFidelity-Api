using Dapper;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var connectionString = app.Configuration.GetConnectionString("HighFidelity")
    ?? throw new InvalidOperationException("Connection string 'HighFidelity' is missing.");

// One generic helper: open a connection per request (SqlClient pools them),
// run the query, return the rows as JSON (camelCase by ASP.NET Core default).
async Task<IResult> QueryAsync<T>(string sql)
{
    await using var connection = new SqlConnection(connectionString);
    var rows = await connection.QueryAsync<T>(sql);
    return Results.Ok(rows);
}

app.MapGet("/api/dashboard/cards", () =>
    QueryAsync<DashboardCardDto>(
        "SELECT Title, Amount, AmountDisplay, Icon, ThemeColorHex FROM dbo.DashboardCards ORDER BY Id"));

app.MapGet("/api/dashboard/revenue-cards", () =>
    QueryAsync<RevenueCardDto>(
        "SELECT Title, Value, Subtitle, ChartType, BackgroundHex, AccentHex FROM dbo.RevenueCards ORDER BY Id"));

app.MapGet("/api/dashboard/activities", () =>
    QueryAsync<ActivityDto>(
        "SELECT Title, Actor, Action, Time, Icon, IconColorHex FROM dbo.Activities ORDER BY Id"));

app.MapGet("/api/dashboard/orders", () =>
    QueryAsync<OrderDto>(
        "SELECT Id, Invoice, Customer, Country, Price, Status FROM dbo.Orders ORDER BY Id"));

// Instant Add: the invoice number is assigned by the database (MAX+1) so
// concurrent clients can't hand out the same number from stale in-memory state.
app.MapPost("/api/dashboard/orders", async (NewOrderRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Customer) || string.IsNullOrWhiteSpace(request.Country))
        return Results.BadRequest(new { error = "Customer and Country are required." });

    await using var connection = new SqlConnection(connectionString);
    var created = await connection.QuerySingleAsync<OrderDto>("""
        INSERT INTO dbo.Orders (Invoice, Customer, Country, Price, Status)
        OUTPUT INSERTED.Id, INSERTED.Invoice, INSERTED.Customer, INSERTED.Country, INSERTED.Price, INSERTED.Status
        SELECT ISNULL(MAX(Invoice), 12411) + 1, @Customer, @Country, @Price, @Status FROM dbo.Orders;
        """, request);
    return Results.Created($"/api/dashboard/orders/{created.Id}", created);
});

// Bulk delete by primary key: DELETE /api/dashboard/orders?ids=3&ids=17
app.MapDelete("/api/dashboard/orders", async (int[] ids) =>
{
    if (ids.Length == 0)
        return Results.BadRequest(new { error = "Provide at least one id." });

    await using var connection = new SqlConnection(connectionString);
    var deleted = await connection.ExecuteAsync(
        "DELETE FROM dbo.Orders WHERE Id IN @Ids", new { Ids = ids });
    return Results.Ok(new { deleted });
});

app.MapGet("/api/dashboard/traffic", () =>
    QueryAsync<TrafficDto>(
        "SELECT Source, Percentage, SegmentColorHex FROM dbo.TrafficSources ORDER BY Id"));

// Simple liveness probe for smoke-testing the deployment.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

// DTOs mirror the MAUI app's Models exactly (property names must match so the
// client's case-insensitive System.Text.Json deserialization binds them).
record DashboardCardDto(string Title, decimal Amount, string AmountDisplay, string Icon, string ThemeColorHex);
record RevenueCardDto(string Title, string Value, string? Subtitle, string ChartType, string BackgroundHex, string AccentHex);
record ActivityDto(string Title, string Actor, string Action, string Time, string Icon, string IconColorHex);
record OrderDto(int Id, int Invoice, string Customer, string Country, decimal Price, string Status);
record NewOrderRequest(string Customer, string Country, decimal Price, string Status);
record TrafficDto(string Source, double Percentage, string SegmentColorHex);
