namespace LimsControlLab.Api.Controllers;

public sealed record ResultReviewDto
{
    public required int Id { get; init; }
    public required int SampleId { get; init; }
    public required string SampleIdentifier { get; init; }
    public required int TemplateId { get; init; }
    public required string TemplateName { get; init; }
    public required string Site { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset StartedAtUtc { get; init; }
    public DateTimeOffset? CompletedAtUtc { get; init; }
    public required int StartedByUserId { get; init; }
    public required bool IsLocked { get; init; }
    public DateTimeOffset? LockedAtUtc { get; init; }
    public int? LockedByUserId { get; init; }
    public required ExceptionDetailDto[] Exceptions { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record ExceptionDetailDto
{
    public required int Id { get; init; }
    public required int ReadingId { get; init; }
    public required string Reason { get; init; }
    public string? Decision { get; init; }
    public string? DecisionComment { get; init; }
    public int? DecidedByUserId { get; init; }
    public DateTimeOffset? DecidedAtUtc { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record UnlockResultRequest
{
    public required string Justification { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record UnlockResultDto
{
    public required int Id { get; init; }
    public required bool IsLocked { get; init; }
    public required string RowVersion { get; init; }
}
