namespace LimsControlLab.Api.Controllers;

public sealed record ResultComparisonRequest
{
    public string? TemplateName { get; init; }
    public int? TestId { get; init; }
    public string? SampleIdentifier { get; init; }
    public DateTimeOffset? FromUtc { get; init; }
    public DateTimeOffset? ToUtc { get; init; }
}

public sealed record ResultComparisonResponse
{
    public string? Unit { get; init; }
    public decimal? ToleranceMin { get; init; }
    public decimal? ToleranceMax { get; init; }
    public required int TotalPoints { get; init; }
    public required IReadOnlyList<ResultComparisonPointDto> Points { get; init; }
}

public sealed record ResultComparisonPointDto
{
    public required int AnalysisId { get; init; }
    public required int SampleId { get; init; }
    public required string SampleIdentifier { get; init; }
    public required string TemplateName { get; init; }
    public int? TestId { get; init; }
    public required decimal Value { get; init; }
    public string? Unit { get; init; }
    public required DateTimeOffset CapturedAtUtc { get; init; }
    public string? ValidationResult { get; init; }
    public decimal? CalibratedValue { get; init; }
}
