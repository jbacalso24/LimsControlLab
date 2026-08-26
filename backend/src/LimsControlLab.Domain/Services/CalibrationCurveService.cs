using LimsControlLab.Domain.Auditing;
using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Calculations;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Services;

public sealed class CalibrationCurveService
{
    private readonly ICalibrationCurveRepository _repository;
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public CalibrationCurveService(
        ICalibrationCurveRepository repository,
        IAnalysisRepository analysisRepository,
        IAuditLogger auditLogger,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _analysisRepository = analysisRepository;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Outcome<CalibrationCurveDto>> CreateAsync(
        string name,
        int analysisTemplateId,
        List<(decimal XValue, decimal YValue)> points,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (_currentUser.Role != Role.LabCoordinator)
            return new Outcome<CalibrationCurveDto>.Forbidden("Only Lab Coordinators can create calibration curves.");

        if (string.IsNullOrWhiteSpace(name))
            return new Outcome<CalibrationCurveDto>.Invalid("name", "Calibration curve name is required.");

        if (points == null || points.Count == 0)
            return new Outcome<CalibrationCurveDto>.Invalid("points", "At least one calibration point is required.");

        var curvePoints = new List<CalibrationPoint>();
        for (int i = 0; i < points.Count; i++)
        {
            curvePoints.Add(new CalibrationPoint
            {
                XValue = points[i].XValue,
                YValue = points[i].YValue,
                Order = i,
            });
        }

        var curve = new CalibrationCurve
        {
            Name = name,
            AnalysisTemplateId = analysisTemplateId,
            IsActive = true,
            Points = curvePoints,
        };

        await _repository.AddAsync(curve, ct);

        await _auditLogger.LogAsync(new AuditLogEntryRecord
        {
            UserId = _currentUser.UserId,
            Role = _currentUser.Role.ToString(),
            TimestampUtc = _timeProvider.GetUtcNow(),
            Action = "CalibrationCurveCreated",
            EntityType = "CalibrationCurve",
            EntityId = curve.Id,
            AfterValues = $"Name: {curve.Name}, Points: {curvePoints.Count}",
        }, ct);

        return new Outcome<CalibrationCurveDto>.Ok(new CalibrationCurveDto
        {
            Id = curve.Id,
            Name = curve.Name,
            AnalysisTemplateId = curve.AnalysisTemplateId,
            IsActive = curve.IsActive,
            PointCount = curvePoints.Count,
            RowVersion = Convert.ToBase64String(curve.RowVersion),
        });
    }

    public async Task<Outcome<CalibrationCurveDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var curve = await _repository.GetByIdAsync(id, ct);
        if (curve == null)
            return new Outcome<CalibrationCurveDto>.NotFound($"Calibration curve {id} not found.");

        return new Outcome<CalibrationCurveDto>.Ok(new CalibrationCurveDto
        {
            Id = curve.Id,
            Name = curve.Name,
            AnalysisTemplateId = curve.AnalysisTemplateId,
            IsActive = curve.IsActive,
            PointCount = curve.Points.Count,
            RowVersion = Convert.ToBase64String(curve.RowVersion),
        });
    }

    public async Task<Outcome<List<CalibrationCurveView>>> ListAsync(CancellationToken ct)
    {
        var curves = await _repository.ListAsync(ct);

        var views = curves.Select(curve => new CalibrationCurveView
        {
            Id = curve.Id,
            Name = curve.Name,
            AnalysisTemplateId = curve.AnalysisTemplateId,
            TemplateName = curve.AnalysisTemplate?.Name ?? string.Empty,
            Site = curve.AnalysisTemplate?.Site.ToString() ?? string.Empty,
            IsActive = curve.IsActive,
            Points = curve.Points
                .OrderBy(p => p.Order)
                .Select(p => new CalibrationPointView { XValue = p.XValue, YValue = p.YValue })
                .ToList(),
            RowVersion = Convert.ToBase64String(curve.RowVersion),
        }).ToList();

        return new Outcome<List<CalibrationCurveView>>.Ok(views);
    }

    public async Task<Outcome<bool>> DeactivateAsync(int id, CancellationToken ct)
    {
        if (_currentUser.Role != Role.LabCoordinator)
            return new Outcome<bool>.Forbidden("Only Lab Coordinators can deactivate calibration curves.");

        var curve = await _repository.GetByIdAsync(id, ct);
        if (curve == null)
            return new Outcome<bool>.NotFound($"Calibration curve {id} not found.");

        curve.IsActive = false;
        await _repository.UpdateAsync(curve, ct);

        await _auditLogger.LogAsync(new AuditLogEntryRecord
        {
            UserId = _currentUser.UserId,
            Role = _currentUser.Role.ToString(),
            TimestampUtc = _timeProvider.GetUtcNow(),
            Action = "CalibrationCurveDeactivated",
            EntityType = "CalibrationCurve",
            EntityId = curve.Id,
            AfterValues = "IsActive: false",
        }, ct);

        return new Outcome<bool>.Ok(true);
    }
}

public sealed record CalibrationCurveDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required int AnalysisTemplateId { get; init; }
    public required bool IsActive { get; init; }
    public required int PointCount { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record CalibrationCurveView
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required int AnalysisTemplateId { get; init; }
    public required string TemplateName { get; init; }
    public required string Site { get; init; }
    public required bool IsActive { get; init; }
    public required List<CalibrationPointView> Points { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record CalibrationPointView
{
    public required decimal XValue { get; init; }
    public required decimal YValue { get; init; }
}
