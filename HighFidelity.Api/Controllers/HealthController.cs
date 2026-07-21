using Microsoft.AspNetCore.Mvc;

namespace HighFidelity.Api.Controllers;

/// <summary>
/// Readiness probe. This is the demo-in-memory branch — there's no database
/// to ping, so "healthy" just means the process is up and serving requests.
/// See the main branch's HealthController for the version that actually
/// checks SQL Server connectivity.
/// </summary>
[ApiController]
[Route("health")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult Get() => Ok(new { status = "ok", database = "n/a (in-memory demo mode)" });
}
