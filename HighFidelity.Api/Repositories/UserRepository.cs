using Microsoft.EntityFrameworkCore;
using HighFidelity.Api.Data;
using HighFidelity.Api.Models;

namespace HighFidelity.Api.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public async Task<User?> GetByUsernameAsync(string username) =>
        await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
}
