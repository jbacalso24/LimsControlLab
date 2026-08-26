namespace LimsControlLab.Domain.Entities;

/// <summary>
/// A named calibration curve with x/y points for interpolation lookups (R40).
/// Multiple CalibrationPoint rows define the curve; lookups interpolate between nearest points.
/// </summary>
public sealed class CalibrationCurve
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required int AnalysisTemplateId { get; set; }
    public required bool IsActive { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public AnalysisTemplate? AnalysisTemplate { get; set; }
    public ICollection<CalibrationPoint> Points { get; set; } = [];
}
