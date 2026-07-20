using HighFidelity.Api.Models;

namespace HighFidelity.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
}
