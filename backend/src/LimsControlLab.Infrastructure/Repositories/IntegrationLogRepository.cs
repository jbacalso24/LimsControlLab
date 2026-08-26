using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LimsControlLab.Infrastructure.Repositories;

public sealed class IntegrationLogRepository : IIntegrationLogRepository
{
    private readonly LimsDbContext _context;

    public IntegrationLogRepository(LimsDbContext context)
    {
        _context = context;
    }

    public async Task<IntegrationLogEntry?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.IntegrationLogs.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task AddAsync(IntegrationLogEntry entry, CancellationToken ct = default)
    {
        _context.IntegrationLogs.Add(entry);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(IntegrationLogEntry entry, CancellationToken ct = default)
    {
        _context.IntegrationLogs.Update(entry);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<IntegrationLogEntry>> GetFailedEntriesAsync(CancellationToken ct = default)
    {
        return await _context.IntegrationLogs
            .Where(x => x.Status == "Failed")
            .OrderBy(x => x.AttemptedAtUtc)
            .ToListAsync(ct);
    }
}
