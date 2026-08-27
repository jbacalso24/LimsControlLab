using LimsControlLab.Domain.Entities;
using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Repositories;

public interface IScheduleRepository
{
    Task<Schedule?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<Schedule>> ListBySiteAsync(Site site, CancellationToken ct = default);
    Task<List<AnalysisAdherenceMarker>> GetAnalysisMarkersAsync(Site site, DateTimeOffset sinceUtc, CancellationToken ct = default);
    void Add(Schedule schedule);
    void Update(Schedule schedule);
    void Remove(Schedule schedule);
}

/// <summary>
/// Lightweight projection of an Analysis used to derive schedule adherence (BRD 6.2).
/// </summary>
public sealed record AnalysisAdherenceMarker(string TemplateName, DateTimeOffset StartedAtUtc);
