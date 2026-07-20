using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
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

// ── Swagger ──
// Left on in every environment (not just Development) so it's always at
// /swagger regardless of how the app is launched — this is a demo API, not
// a service with something to hide behind an environment check.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "HighFidelity.Api", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the token from POST /api/auth/login (no 'Bearer ' prefix needed here)."
    });
    options.AddSecurityRequirement(document =>
    {
        var bearerRef = new OpenApiSecuritySchemeReference("Bearer", document);
        return new OpenApiSecurityRequirement { [bearerRef] = [] };
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "HighFidelity.Api v1"));

// Bare root has no page of its own — send visitors to the interactive docs
// instead of a bare 404.
app.MapGet("/", () => Results.Redirect("/swagger"));

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
