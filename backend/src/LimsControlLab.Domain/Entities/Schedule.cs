using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Entities;

public sealed class Schedule
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required Site Site { get; set; }
    public string? AnalysisType { get; set; }
    public required ShiftPattern ShiftPattern { get; set; }
    public string? RecurrencePattern { get; set; }
    public string? ExclusionRules { get; set; }
    public int? AssignedToUserId { get; set; }
    public required bool IsActive { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public User? AssignedToUser { get; set; }
}
