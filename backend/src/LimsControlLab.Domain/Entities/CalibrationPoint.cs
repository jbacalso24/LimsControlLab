namespace LimsControlLab.Domain.Entities;

/// <summary>
/// A single x/y point in a CalibrationCurve, used for interpolation lookups.
/// X = input value, Y = calibrated/adjusted output value.
/// </summary>
public sealed class CalibrationPoint
{
    public int Id { get; set; }
    public int CalibrationCurveId { get; set; }
    public required decimal XValue { get; set; }
    public required decimal YValue { get; set; }
    public required int Order { get; set; }

    public CalibrationCurve? CalibrationCurve { get; set; }
}
