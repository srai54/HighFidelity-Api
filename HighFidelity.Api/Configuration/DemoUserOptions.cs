namespace HighFidelity.Api.Configuration;

/// <summary>
/// Bound from the "DemoUser" section of appsettings.json.
/// Stand-in for a real Users table — see docs/ARCHITECTURE.md for how to
/// replace this with EF-backed users and hashed passwords.
/// </summary>
public class DemoUserOptions
{
    public const string SectionName = "DemoUser";

    public required string Username { get; init; }
    public required string Password { get; init; }
}
