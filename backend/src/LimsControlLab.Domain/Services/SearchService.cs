using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Repositories;

namespace LimsControlLab.Domain.Services;

public sealed class SearchService
{
    private readonly ISearchRepository _searchRepository;
    private readonly ICurrentUser _currentUser;

    public SearchService(ISearchRepository searchRepository, ICurrentUser currentUser)
    {
        _searchRepository = searchRepository;
        _currentUser = currentUser;
    }

    public IQueryable<SearchResultDto> Search(SearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var filter = new SearchFilter
        {
            Site = _currentUser.Site,
            TemplateName = request.TemplateName,
            TestId = request.TestId,
            InstrumentId = request.InstrumentId,
            SampleIdentifier = request.SampleIdentifier,
            FromUtc = request.FromUtc,
            ToUtc = request.ToUtc,
        };

        var results = _searchRepository.Search(filter);

        var dtos = results.Select(r => new SearchResultDto
        {
            AnalysisId = r.AnalysisId,
            SampleId = r.SampleId,
            SampleIdentifier = r.SampleIdentifier,
            TemplateName = r.TemplateName,
            Site = r.Site.ToString(),
            Status = r.Status.ToString(),
            IsLocked = r.IsLocked,
            StartedAtUtc = r.StartedAtUtc,
            CompletedAtUtc = r.CompletedAtUtc,
            ReadingId = r.ReadingId,
            TestId = r.TestId,
            ReadingValue = r.ReadingValue,
            ReadingUnit = r.ReadingUnit,
            CapturedAtUtc = r.CapturedAtUtc,
            ValidationResult = r.ValidationResult,
            CalibratedValue = r.CalibratedValue,
            InstrumentId = r.InstrumentId,
        });

        return dtos;
    }
}

public sealed record SearchRequest
{
    public string? TemplateName { get; init; }
    public int? TestId { get; init; }
    public int? InstrumentId { get; init; }
    public string? SampleIdentifier { get; init; }
    public DateTimeOffset? FromUtc { get; init; }
    public DateTimeOffset? ToUtc { get; init; }
}

public sealed record SearchResultDto
{
    public required int AnalysisId { get; init; }
    public required int SampleId { get; init; }
    public required string SampleIdentifier { get; init; }
    public required string TemplateName { get; init; }
    public required string Site { get; init; }
    public required string Status { get; init; }
    public required bool IsLocked { get; init; }
    public required DateTimeOffset StartedAtUtc { get; init; }
    public DateTimeOffset? CompletedAtUtc { get; init; }

    public int? ReadingId { get; init; }
    public int? TestId { get; init; }
    public decimal? ReadingValue { get; init; }
    public string? ReadingUnit { get; init; }
    public DateTimeOffset? CapturedAtUtc { get; init; }
    public string? ValidationResult { get; init; }
    public decimal? CalibratedValue { get; init; }
    public int? InstrumentId { get; init; }
}
