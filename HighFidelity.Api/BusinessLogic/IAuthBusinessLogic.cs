using HighFidelity.Api.DTOs;

namespace HighFidelity.Api.BusinessLogic;

public interface IAuthBusinessLogic
{
    /// <summary>Validates credentials and issues a JWT. Returns null on a bad username/password.</summary>
    Task<LoginResponseDto?> LoginAsync(string username, string password);
}
