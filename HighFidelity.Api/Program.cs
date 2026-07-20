using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using HighFidelity.Api.Configuration;
using HighFidelity.Api.Data;
using HighFidelity.Api.Repositories;
using HighFidelity.Api.BusinessLogic;

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
//   Controller → BusinessLogic (BL) → Repository → DbContext → SQL Server
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IDashboardBusinessLogic, DashboardBusinessLogic>();
builder.Services.AddScoped<IAuthBusinessLogic, AuthBusinessLogic>();

// ── JWT Authentication ──
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.Configure<JwtOptions>(jwtSection);
builder.Services.Configure<DemoUserOptions>(builder.Configuration.GetSection(DemoUserOptions.SectionName));

var jwtOptions = jwtSection.Get<JwtOptions>()
    ?? throw new InvalidOperationException("Configuration section 'Jwt' is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
