using LimsControlLab.Domain.Auditing;
using LimsControlLab.Domain.Entities;

namespace LimsControlLab.Infrastructure;

public sealed class AuditLogger : IAuditLogger
{
    private readonly LimsDbContext _context;

    public AuditLogger(LimsDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(AuditLogEntryRecord entry, CancellationToken ct = default)
    {
        var logEntry = new AuditLogEntry
        {
            UserId = entry.UserId,
            Role = entry.Role,
            TimestampUtc = entry.TimestampUtc,
            Action = entry.Action,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            BeforeValues = entry.BeforeValues,
            AfterValues = entry.AfterValues,
            CorrelationId = entry.CorrelationId,
        };

        _context.AuditLogs.Add(logEntry);
        await _context.SaveChangesAsync(ct);
    }
}
