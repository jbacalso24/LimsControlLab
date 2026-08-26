using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Entities;

/// <summary>
/// Analysis template configuration, reusable across sites, products, and roles (R1, R2, R4).
/// When modified, a new AnalysisTemplateVersion is created; existing analyses remain unaffected (R5).
/// MinTolerance/MaxTolerance kept here for backwards compatibility; also versioned in AnalysisTemplateVersion.
/// </summary>
public sealed class AnalysisTemplate
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required Site Site { get; set; }
    public int? CurrentVersionId { get; set; }
    public required bool IsRetired { get; set; }
    public decimal? MinTolerance { get; set; }
    public decimal? MaxTolerance { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public AnalysisTemplateVersion? CurrentVersion { get; set; }
    public ICollection<AnalysisTemplateVersion> Versions { get; set; } = [];
}
