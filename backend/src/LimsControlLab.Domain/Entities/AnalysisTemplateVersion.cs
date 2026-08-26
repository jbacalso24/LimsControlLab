namespace LimsControlLab.Domain.Entities;

/// <summary>
/// A version snapshot of an analysis template configuration.
/// Enables R5: modifications to a template don't affect analyses already using an earlier version.
/// </summary>
public sealed class AnalysisTemplateVersion
{
    public int Id { get; set; }
    public required int TemplateId { get; set; }
    public required int Version { get; set; }
    public string? TestConfiguration { get; set; }
    public string? CalculationDefinitions { get; set; }
    public string? ValidationRules { get; set; }
    public decimal? MinTolerance { get; set; }
    public decimal? MaxTolerance { get; set; }
    public required DateTimeOffset CreatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public AnalysisTemplate? Template { get; set; }
}
