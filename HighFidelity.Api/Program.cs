using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using HighFidelity.Api.Data;
using HighFidelity.Api.Repositories;
using HighFidelity.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ──
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // camelCase to match the MAUI client's deserialization expectations.
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ── EF Core ──
var connectionString = builder.Configuration.GetConnectionString("HighFidelity")
    ?? throw new InvalidOperationException("Connection string 'HighFidelity' is missing.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
        // Transient network/deadlock errors retry automatically instead of
        // surfacing as a 500 on the first blip.
        sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null)));

// ── Dependency Injection — Layered Architecture ──
// Each layer depends on the one below it through interfaces:
//   Controller → Service (BL) → Repository → DbContext → SQL Server
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

// ── Centralized error handling ──
// Any exception that escapes a controller (DB unreachable, unexpected bug)
// is converted to a consistent JSON error instead of leaking a raw stack
// trace or an empty connection-reset response.
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var feature = context.Features.Get<IExceptionHandlerFeature>();
    context.Response.ContentType = "application/json";
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new
    {
        error = "An unexpected error occurred.",
        detail = app.Environment.IsDevelopment() ? feature?.Error.Message : null
    });
}));

app.MapControllers();

app.Run();
