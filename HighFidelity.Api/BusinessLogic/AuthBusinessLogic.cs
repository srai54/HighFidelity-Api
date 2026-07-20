using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HighFidelity.Api.Configuration;
using HighFidelity.Api.DTOs;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HighFidelity.Api.BusinessLogic;

/// <summary>
/// Demo credential check + JWT issuing. There's no Users table yet, so this
/// validates against the single account in the "DemoUser" config section —
/// see docs/ARCHITECTURE.md for how to swap this for an EF-backed user store.
/// </summary>
public class AuthBusinessLogic : IAuthBusinessLogic
{
    private readonly JwtOptions _jwtOptions;
    private readonly DemoUserOptions _demoUser;

    public AuthBusinessLogic(IOptions<JwtOptions> jwtOptions, IOptions<DemoUserOptions> demoUser)
    {
        _jwtOptions = jwtOptions.Value;
        _demoUser = demoUser.Value;
    }

    public LoginResponseDto? Login(string username, string password)
    {
        if (!FixedTimeEquals(username, _demoUser.Username) || !FixedTimeEquals(password, _demoUser.Password))
            return null;

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return new LoginResponseDto(tokenString, expiresAtUtc);
    }

    // Avoids leaking username/password validity via response-time differences.
    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
