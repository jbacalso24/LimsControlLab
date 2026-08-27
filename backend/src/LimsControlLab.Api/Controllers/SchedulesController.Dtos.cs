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

public sealed record ScheduleAdherenceResponse
{
    public required DateTimeOffset AsOfUtc { get; init; }
    public required AdherenceSummaryDto Summary { get; init; }
    public required List<ScheduleAdherenceItemDto> Schedules { get; init; }
}

public sealed record AdherenceSummaryDto
{
    public required int OnTrack { get; init; }
    public required int Due { get; init; }
    public required int Overdue { get; init; }
    public required int Missed { get; init; }
    public required int Total { get; init; }
}

public sealed record ScheduleAdherenceItemDto
{
    public required int ScheduleId { get; init; }
    public required string Name { get; init; }
    public string? AnalysisType { get; init; }
    public required string ShiftPattern { get; init; }
    public required string CadenceLabel { get; init; }
    public required string Status { get; init; }
    public int? AssignedToUserId { get; init; }
    public string? AssignedToUsername { get; init; }
    public DateTimeOffset? LastAnalysisAtUtc { get; init; }
    public required int MissedPeriods { get; init; }
    public required DateTimeOffset CurrentPeriodStartUtc { get; init; }
    public required DateTimeOffset CurrentPeriodEndUtc { get; init; }
}
