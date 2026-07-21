using HighFidelity.Api.Models;

namespace HighFidelity.Api.Repositories;

/// <summary>
/// Hardcoded stand-in for <see cref="UserRepository"/> — same demo account as
/// database/seed.sql (admin / ChangeMe123!), same PasswordHasher&lt;T&gt; hash,
/// just held in memory instead of a Users table. AuthBusinessLogic doesn't
/// know or care which one it's talking to.
/// </summary>
public class InMemoryUserRepository : IUserRepository
{
    private static readonly User DemoUser = new()
    {
        Id = 1,
        Username = "admin",
        PasswordHash = "AQAAAAIAAYagAAAAEIK69S7+L1t/3oVtC9tpAKXRVITRdEY5cPoj66owFv6iU+lcR6gdyfdPqqQ/cK5ziQ==",
        CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    public Task<User?> GetByUsernameAsync(string username) =>
        Task.FromResult(username == DemoUser.Username ? DemoUser : null);
}
