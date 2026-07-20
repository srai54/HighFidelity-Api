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
    options.UseSqlServer(connectionString));

// ── Dependency Injection — Layered Architecture ──
// Each layer depends on the one below it through interfaces:
//   Controller → Service (BL) → Repository → DbContext → SQL Server
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

app.MapControllers();

app.Run();
