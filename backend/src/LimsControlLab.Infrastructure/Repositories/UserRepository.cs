using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LimsControlLab.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly LimsDbContext _db;

    public UserRepository(LimsDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
    }
}
