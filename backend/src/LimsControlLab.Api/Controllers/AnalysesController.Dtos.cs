namespace LimsControlLab.Api.Controllers;

public sealed record ReadingValidationDto
{
    public required bool IsValid { get; init; }
    public string? ExpectedRange { get; init; }
    public required string ActualValue { get; init; }
    public string? Reason { get; init; }
}

public sealed record ReadingDto
{
    public required int Id { get; init; }
    public required int TestId { get; init; }
    public required decimal Value { get; init; }
    public required string Unit { get; init; }
    public required DateTimeOffset CapturedAtUtc { get; init; }
    public required string CapturedBy { get; init; }
    public required string CapturedByUsername { get; init; }
    public required ReadingValidationDto ValidationResult { get; init; }
    public decimal? CalibratedValue { get; init; }
}

public sealed record ExceptionDto
{
    public required int Id { get; init; }
    public required int ReadingId { get; init; }
    public required string Reason { get; init; }
    public string? Decision { get; init; }
    public string? DecisionComment { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record AnalysisStatusDto
{
    public required int Id { get; init; }
    public required string Status { get; init; }
    public required bool IsLocked { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record AnalysisDetailDto
{
    public required int Id { get; init; }
    public required int SampleId { get; init; }
    public required int TemplateId { get; init; }
    public required string Status { get; init; }
    public required bool IsLocked { get; init; }
    public required List<ReadingDto> Readings { get; init; }
    public required List<ExceptionDto> Exceptions { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record CreateReadingRequest
{
    public required int TestId { get; init; }
    public required decimal Value { get; init; }
    public required string Unit { get; init; }
    public required DateTimeOffset CapturedAtUtc { get; init; }
    public int? InstrumentId { get; init; }
}

public sealed record ExceptionDecisionRequest
{
    public required string Decision { get; init; }
    public required string Comment { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record StatusChangeRequest
{
    public required string Action { get; init; }
    public required string RowVersion { get; init; }
}
