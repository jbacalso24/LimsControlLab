using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LimsControlLab.Infrastructure.Repositories;

public sealed class CalibrationCurveRepository : ICalibrationCurveRepository
{
    private readonly LimsDbContext _db;

    public CalibrationCurveRepository(LimsDbContext db)
    {
        _db = db;
    }

    public async Task<CalibrationCurve?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.CalibrationCurves
            .Include(c => c.Points.OrderBy(p => p.Order))
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<CalibrationCurve?> GetByAnalysisTemplateIdAsync(int templateId, CancellationToken ct = default)
    {
        return await _db.CalibrationCurves
            .Include(c => c.Points.OrderBy(p => p.Order))
            .FirstOrDefaultAsync(c => c.AnalysisTemplateId == templateId && c.IsActive, ct);
    }

    public async Task<IReadOnlyList<CalibrationCurve>> ListAsync(CancellationToken ct = default)
    {
        return await _db.CalibrationCurves
            .Include(c => c.AnalysisTemplate)
            .Include(c => c.Points.OrderBy(p => p.Order))
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task AddAsync(CalibrationCurve curve, CancellationToken ct = default)
    {
        _db.CalibrationCurves.Add(curve);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(CalibrationCurve curve, CancellationToken ct = default)
    {
        _db.CalibrationCurves.Update(curve);
        await _db.SaveChangesAsync(ct);
    }
}
