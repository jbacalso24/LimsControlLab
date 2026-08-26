namespace LimsControlLab.Api.Controllers;

public sealed record SearchResultsRequest
{
    public string? TemplateName { get; init; }
    public int? TestId { get; init; }
    public int? InstrumentId { get; init; }
    public string? SampleIdentifier { get; init; }
    public DateTimeOffset? FromUtc { get; init; }
    public DateTimeOffset? ToUtc { get; init; }
}

public sealed record SearchResultItemDto
{
    public required int AnalysisId { get; init; }
    public required int SampleId { get; init; }
    public required string SampleIdentifier { get; init; }
    public required string TemplateName { get; init; }
    public required string Site { get; init; }
    public required string Status { get; init; }
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
