using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Entities;

public sealed class Sample
{
    public int Id { get; set; }
    public required string Identifier { get; set; }
    public required int AnalysisTemplateId { get; set; }
    public required LifecycleStatus Status { get; set; }
    public required Site Site { get; set; }
    public required Site CurrentSite { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public AnalysisTemplate? AnalysisTemplate { get; set; }
    public ICollection<SampleTransfer> Transfers { get; set; } = [];
}
