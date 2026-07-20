using HighFidelity.Api.DTOs;

namespace HighFidelity.Api.BusinessLogic;

public interface IAuthBusinessLogic
{
    /// <summary>Validates credentials and issues a JWT. Returns null on a bad username/password.</summary>
    LoginResponseDto? Login(string username, string password);
}
