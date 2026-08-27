using LimsControlLab.Domain.Entities;

namespace LimsControlLab.Domain.Repositories;

public interface IIntegrationLogRepository
{
    Task<IntegrationLogEntry?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(IntegrationLogEntry entry, CancellationToken ct = default);
    Task UpdateAsync(IntegrationLogEntry entry, CancellationToken ct = default);
    Task<IEnumerable<IntegrationLogEntry>> GetFailedEntriesAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns integration attempts (newest first), optionally filtered by status and/or target system.
    /// </summary>
    Task<IReadOnlyList<IntegrationLogEntry>> ListAsync(string? status, string? targetSystem, CancellationToken ct = default);
}
