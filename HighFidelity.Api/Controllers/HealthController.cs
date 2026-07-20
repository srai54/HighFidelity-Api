using Microsoft.AspNetCore.Mvc;
using HighFidelity.Api.Data;

namespace HighFidelity.Api.Controllers;

/// <summary>
/// Readiness probe for deployment health checks and smoke tests.
/// Actually pings the database — a process that's "up" but can't reach SQL
/// Server is not healthy, and a probe that only checks the process would miss that.
/// </summary>
[ApiController]
[Route("health")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult> Get()
    {
        var dbReachable = await _db.Database.CanConnectAsync();
        var status = new { status = dbReachable ? "ok" : "degraded", database = dbReachable ? "connected" : "unreachable" };
        return dbReachable ? Ok(status) : StatusCode(StatusCodes.Status503ServiceUnavailable, status);
    }
}
