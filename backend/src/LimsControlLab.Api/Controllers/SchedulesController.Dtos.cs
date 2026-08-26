namespace LimsControlLab.Api.Controllers;

public sealed record CreateScheduleRequest
{
    public required string Name { get; init; }
    public required string Site { get; init; }
    public string? AnalysisType { get; init; }
    public required string ShiftPattern { get; init; }
    public string? RecurrencePattern { get; init; }
    public string? ExclusionRules { get; init; }
    public int? AssignedToUserId { get; init; }
}

public sealed record UpdateScheduleRequest
{
    public required string Name { get; init; }
    public string? AnalysisType { get; init; }
    public required string ShiftPattern { get; init; }
    public string? RecurrencePattern { get; init; }
    public string? ExclusionRules { get; init; }
    public int? AssignedToUserId { get; init; }
    public required bool IsActive { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record ScheduleDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Site { get; init; }
    public string? AnalysisType { get; init; }
    public required string ShiftPattern { get; init; }
    public string? RecurrencePattern { get; init; }
    public string? ExclusionRules { get; init; }
    public int? AssignedToUserId { get; init; }
    public required bool IsActive { get; init; }
    public required string RowVersion { get; init; }
}
