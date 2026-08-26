using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Entities;

public sealed class Analysis
{
    public int Id { get; set; }
    public required int SampleId { get; set; }
    public required int TemplateId { get; set; }
    public required int TemplateVersionId { get; set; }
    public required LifecycleStatus Status { get; set; }
    public required DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public required int StartedByUserId { get; set; }
    public required bool IsLocked { get; set; }
    public DateTimeOffset? LockedAtUtc { get; set; }
    public int? LockedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Sample? Sample { get; set; }
    public AnalysisTemplate? Template { get; set; }
    public AnalysisTemplateVersion? TemplateVersion { get; set; }
    public ICollection<Reading> Readings { get; set; } = [];
    public ICollection<ExceptionRecord> Exceptions { get; set; } = [];
}
