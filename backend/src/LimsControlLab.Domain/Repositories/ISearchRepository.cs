using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Repositories;

public interface ISearchRepository
{
    IQueryable<SearchResult> Search(SearchFilter filter);
}

public sealed record SearchFilter
{
    public required Site Site { get; init; }
    public string? TemplateName { get; init; }
    public int? TestId { get; init; }
    public int? InstrumentId { get; init; }
    public string? SampleIdentifier { get; init; }
    public DateTimeOffset? FromUtc { get; init; }
    public DateTimeOffset? ToUtc { get; init; }
}

public sealed record SearchResult
{
    public required int AnalysisId { get; init; }
    public required int SampleId { get; init; }
    public required string SampleIdentifier { get; init; }
    public required string TemplateName { get; init; }
    public required Site Site { get; init; }
    public required LifecycleStatus Status { get; init; }
    public required bool IsLocked { get; init; }
    public required DateTimeOffset StartedAtUtc { get; init; }
    public DateTimeOffset? CompletedAtUtc { get; init; }

    public int? ReadingId { get; init; }
    public int? TestId { get; init; }
    public decimal? ReadingValue { get; init; }
    public string? ReadingUnit { get; init; }
    public DateTimeOffset? CapturedAtUtc { get; init; }
    public string? ValidationResult { get; init; }
    public decimal? CalibratedValue { get; init; }
    public int? InstrumentId { get; init; }
}
