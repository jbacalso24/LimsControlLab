using LimsControlLab.Domain.Entities;

namespace LimsControlLab.Domain.Repositories;

public interface IAnalysisTemplateRepository
{
    Task<AnalysisTemplate?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<AnalysisTemplate>> ListBySiteAsync(global::LimsControlLab.SharedKernel.Enums.Site site, CancellationToken ct = default);
    void Add(AnalysisTemplate entity);
    void Update(AnalysisTemplate entity);
    void AddVersion(AnalysisTemplateVersion version);
}
