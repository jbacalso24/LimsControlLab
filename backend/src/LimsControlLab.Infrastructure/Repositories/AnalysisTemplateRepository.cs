using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;

namespace LimsControlLab.Infrastructure.Repositories;

public sealed class AnalysisTemplateRepository : IAnalysisTemplateRepository
{
    private readonly LimsDbContext _db;

    public AnalysisTemplateRepository(LimsDbContext db)
    {
        _db = db;
    }

    public async Task<AnalysisTemplate?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.AnalysisTemplates
            .Include(t => t.CurrentVersion)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<List<AnalysisTemplate>> ListBySiteAsync(Site site, CancellationToken ct = default)
    {
        return await _db.AnalysisTemplates
            .Where(t => t.Site == site)
            .Include(t => t.CurrentVersion)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public void Add(AnalysisTemplate entity)
    {
        _db.AnalysisTemplates.Add(entity);
    }

    public void Update(AnalysisTemplate entity)
    {
        _db.AnalysisTemplates.Update(entity);
    }

    public void AddVersion(AnalysisTemplateVersion version)
    {
        _db.AnalysisTemplateVersions.Add(version);
    }
}
