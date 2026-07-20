using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HighFidelity.Api.Configuration;
using HighFidelity.Api.DTOs;
using HighFidelity.Api.Models;
using HighFidelity.Api.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HighFidelity.Api.BusinessLogic;

/// <summary>
/// Checks credentials against the "Users" table (PBKDF2 via PasswordHasher&lt;T&gt;,
/// no third-party auth library involved — see docs/ARCHITECTURE.md) and issues a JWT.
/// </summary>
public class AuthBusinessLogic : IAuthBusinessLogic
{
    // Verified against on a "no such user" lookup so a login attempt takes
    // roughly the same time whether or not the username exists — otherwise
    // response time alone reveals which usernames are registered.
    private const string DummyPasswordHash =
        "AQAAAAIAAYagAAAAEIK69S7+L1t/3oVtC9tpAKXRVITRdEY5cPoj66owFv6iU+lcR6gdyfdPqqQ/cK5ziQ==";

    private readonly JwtOptions _jwtOptions;
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthBusinessLogic(IOptions<JwtOptions> jwtOptions, IUserRepository userRepository)
    {
        _jwtOptions = jwtOptions.Value;
        _userRepository = userRepository;
    }

    public async Task<LoginResponseDto?> LoginAsync(string username, string password)
    {
        var user = await _userRepository.GetByUsernameAsync(username);

        var result = _passwordHasher.VerifyHashedPassword(
            user ?? new User(), user?.PasswordHash ?? DummyPasswordHash, password);

        if (user is null || result == PasswordVerificationResult.Failed)
            return null;

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
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
}
