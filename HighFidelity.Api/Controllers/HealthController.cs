using Microsoft.AspNetCore.Mvc;

namespace HighFidelity.Api.Controllers;

/// <summary>
/// Liveness probe for deployment health checks and smoke tests.
/// </summary>
[ApiController]
[Route("health")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult Get() => Ok(new { status = "ok" });
}
