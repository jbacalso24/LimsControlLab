namespace LimsControlLab.Api.Controllers;

public sealed record CreateCalibrationCurveRequest
{
    public required string Name { get; init; }
    public required int AnalysisTemplateId { get; init; }
    public required List<CalibrationPointRequest> Points { get; init; }
}

public sealed record CalibrationPointRequest
{
    public required decimal XValue { get; init; }
    public required decimal YValue { get; init; }
}

public sealed record CalibrationCurveDetailDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required int AnalysisTemplateId { get; init; }
    public required bool IsActive { get; init; }
    public required int PointCount { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record DeactivateCalibrationCurveRequest
{
    public required string RowVersion { get; init; }
}
