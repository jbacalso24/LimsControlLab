using LimsControlLab.Domain.Entities;

namespace LimsControlLab.Domain.Repositories;

public interface IAuditLogRepository
{
    /// <summary>
    /// Returns a page of audit entries (newest first), optionally filtered by entity type and action,
    /// together with the total count matching the filter (for pagination).
    /// </summary>
    Task<AuditLogPage> ListAsync(string? entityType, string? action, int skip, int take, CancellationToken ct = default);
}

public sealed record AuditLogPage
{
    public required IReadOnlyList<AuditLogEntry> Items { get; init; }
    public required int Total { get; init; }
}
