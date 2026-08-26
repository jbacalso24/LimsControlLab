using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LimsControlLab.Infrastructure.Repositories;

public sealed class SearchRepository : ISearchRepository
{
    private readonly LimsDbContext _dbContext;

    public SearchRepository(LimsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<SearchResult> Search(SearchFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var query =
            from a in _dbContext.Analyses
            join s in _dbContext.Samples on a.SampleId equals s.Id
            join t in _dbContext.AnalysisTemplates on a.TemplateId equals t.Id
            where s.Site == filter.Site
            where filter.TemplateName == null || t.Name.Contains(filter.TemplateName)
            where !filter.FromUtc.HasValue || a.StartedAtUtc >= filter.FromUtc
            where !filter.ToUtc.HasValue || a.StartedAtUtc <= filter.ToUtc
            where filter.SampleIdentifier == null || s.Identifier.Contains(filter.SampleIdentifier)
            from r in _dbContext.Readings.Where(x => x.AnalysisId == a.Id).DefaultIfEmpty()
            where r == null || !filter.TestId.HasValue || r.TestId == filter.TestId
            where r == null || !filter.InstrumentId.HasValue || r.InstrumentId == filter.InstrumentId
            orderby a.StartedAtUtc descending
            select new SearchResult
            {
                AnalysisId = a.Id,
                SampleId = s.Id,
                SampleIdentifier = s.Identifier,
                TemplateName = t.Name,
                Site = s.Site,
                Status = a.Status,
                IsLocked = a.IsLocked,
                StartedAtUtc = a.StartedAtUtc,
                CompletedAtUtc = a.CompletedAtUtc,
                ReadingId = r == null ? (int?)null : r.Id,
                TestId = r == null ? (int?)null : r.TestId,
                ReadingValue = r == null ? (decimal?)null : r.Value,
                ReadingUnit = r == null ? null : r.Unit,
                CapturedAtUtc = r == null ? (DateTimeOffset?)null : r.CapturedAtUtc,
                ValidationResult = r == null ? null : r.ValidationResult,
                CalibratedValue = r == null ? (decimal?)null : r.CalibratedValue,
                InstrumentId = r == null ? (int?)null : r.InstrumentId,
            };

        return query;
    }
}
