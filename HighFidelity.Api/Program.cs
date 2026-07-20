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
        "SELECT Invoice, Customer, Country, Price, Status FROM dbo.Orders ORDER BY Id"));

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
record OrderDto(int Invoice, string Customer, string Country, decimal Price, string Status);
record TrafficDto(string Source, double Percentage, string SegmentColorHex);
