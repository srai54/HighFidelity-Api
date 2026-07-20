namespace HighFidelity.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;

    // ASP.NET Core Identity's PasswordHasher<T> format (PBKDF2, salted) —
    // never a plaintext password, never a raw unsalted hash.
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
