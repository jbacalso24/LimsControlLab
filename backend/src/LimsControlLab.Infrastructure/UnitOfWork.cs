namespace LimsControlLab.Infrastructure;

using LimsControlLab.Domain.Common;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly LimsDbContext _context;

    public UnitOfWork(LimsDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
