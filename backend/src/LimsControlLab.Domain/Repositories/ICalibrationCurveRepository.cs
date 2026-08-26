using LimsControlLab.Domain.Entities;

namespace LimsControlLab.Domain.Repositories;

public interface ICalibrationCurveRepository
{
    Task<CalibrationCurve?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CalibrationCurve?> GetByAnalysisTemplateIdAsync(int templateId, CancellationToken ct = default);
    Task<IReadOnlyList<CalibrationCurve>> ListAsync(CancellationToken ct = default);
    Task AddAsync(CalibrationCurve curve, CancellationToken ct = default);
    Task UpdateAsync(CalibrationCurve curve, CancellationToken ct = default);
}
