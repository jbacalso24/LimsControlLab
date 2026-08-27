using LimsControlLab.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LimsControlLab.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly LimsDbContext _db;

    public AuditLogRepository(LimsDbContext db)
    {
        _db = db;
    }

    public async Task<AuditLogPage> ListAsync(string? entityType, string? action, int skip, int take, CancellationToken ct = default)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.TimestampUtc)
            .ThenByDescending(a => a.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return new AuditLogPage { Items = items, Total = total };
    }
}
