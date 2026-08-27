namespace LimsControlLab.Api.Controllers;

public sealed record IntegrationLogDto
{
    public required int Id { get; init; }
    public required string TargetSystem { get; init; }
    public required int AnalysisId { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset AttemptedAtUtc { get; init; }
    public DateTimeOffset? CompletedAtUtc { get; init; }
    public string? ErrorMessage { get; init; }
    public required int RetryCount { get; init; }
}

public sealed record ReprocessResultDto
{
    public required int Id { get; init; }
    public required bool Success { get; init; }
    public required string Status { get; init; }
    public required string Message { get; init; }
}
