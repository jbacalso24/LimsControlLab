using LimsControlLab.Domain.Entities;

namespace LimsControlLab.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
}
