using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Repositories;

namespace LimsControlLab.Domain.Services;

public sealed class ResultComparisonService
{
    private readonly ISearchRepository _searchRepository;
    private readonly IAnalysisTemplateRepository _analysisTemplateRepository;
    private readonly ICurrentUser _currentUser;

    public ResultComparisonService(
        ISearchRepository searchRepository,
        IAnalysisTemplateRepository analysisTemplateRepository,
        ICurrentUser currentUser)
    {
        _searchRepository = searchRepository;
        _analysisTemplateRepository = analysisTemplateRepository;
        _currentUser = currentUser;
    }

    public async Task<Outcome<ResultComparisonResult>> CompareAsync(ResultComparisonQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var filter = new SearchFilter
        {
            Site = _currentUser.Site,
            TemplateName = query.TemplateName,
            TestId = query.TestId,
            SampleIdentifier = query.SampleIdentifier,
            FromUtc = query.FromUtc,
            ToUtc = query.ToUtc,
        };

        var points = _searchRepository.Search(filter)
            .Where(r => r.ReadingValue.HasValue && r.CapturedAtUtc.HasValue)
            .OrderBy(r => r.CapturedAtUtc)
            .Select(r => new ResultComparisonPoint
            {
                AnalysisId = r.AnalysisId,
                SampleId = r.SampleId,
                SampleIdentifier = r.SampleIdentifier,
                TemplateName = r.TemplateName,
                TestId = r.TestId,
                Value = r.ReadingValue!.Value,
                Unit = r.ReadingUnit,
                CapturedAtUtc = r.CapturedAtUtc!.Value,
                ValidationResult = r.ValidationResult,
                CalibratedValue = r.CalibratedValue,
            })
            .ToList();

        var distinctUnits = points.Select(p => p.Unit).Distinct().ToList();
        var unit = distinctUnits.Count == 1 ? distinctUnits[0] : null;

        decimal? toleranceMin = null;
        decimal? toleranceMax = null;
        if (!string.IsNullOrWhiteSpace(query.TemplateName))
        {
            var templates = await _analysisTemplateRepository.ListBySiteAsync(_currentUser.Site, ct);
            var matches = templates
                .Where(t => string.Equals(t.Name, query.TemplateName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (matches.Count == 1)
            {
                toleranceMin = matches[0].MinTolerance;
                toleranceMax = matches[0].MaxTolerance;
            }
        }

        var result = new ResultComparisonResult
        {
            Unit = unit,
            ToleranceMin = toleranceMin,
            ToleranceMax = toleranceMax,
            TotalPoints = points.Count,
            Points = points,
        };

        return new Outcome<ResultComparisonResult>.Ok(result);
    }
}

public sealed record ResultComparisonQuery
{
    public string? TemplateName { get; init; }
    public int? TestId { get; init; }
    public string? SampleIdentifier { get; init; }
    public DateTimeOffset? FromUtc { get; init; }
    public DateTimeOffset? ToUtc { get; init; }
}

public sealed record ResultComparisonResult
{
    public string? Unit { get; init; }
    public decimal? ToleranceMin { get; init; }
    public decimal? ToleranceMax { get; init; }
    public required int TotalPoints { get; init; }
    public required IReadOnlyList<ResultComparisonPoint> Points { get; init; }
}

public sealed record ResultComparisonPoint
{
    public required int AnalysisId { get; init; }
    public required int SampleId { get; init; }
    public required string SampleIdentifier { get; init; }
    public required string TemplateName { get; init; }
    public int? TestId { get; init; }
    public required decimal Value { get; init; }
    public string? Unit { get; init; }
    public required DateTimeOffset CapturedAtUtc { get; init; }
    public string? ValidationResult { get; init; }
    public decimal? CalibratedValue { get; init; }
}
