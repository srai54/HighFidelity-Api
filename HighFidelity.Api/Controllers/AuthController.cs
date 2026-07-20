using HighFidelity.Api.BusinessLogic;
using HighFidelity.Api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace HighFidelity.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthBusinessLogic _authBusinessLogic;

    public AuthController(IAuthBusinessLogic authBusinessLogic) => _authBusinessLogic = authBusinessLogic;

    /// <summary>Exchanges username/password for a JWT bearer token.</summary>
    [HttpPost("login")]
    public ActionResult<LoginResponseDto> Login([FromBody] LoginRequestDto request)
    {
        var result = _authBusinessLogic.Login(request.Username, request.Password);
        return result is null
            ? Unauthorized(new { error = "Invalid username or password." })
            : Ok(result);
    }
}
