using LimsControlLab.Api.Common;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimsControlLab.Api.Controllers;

[ApiController]
[Route("api/v1/search")]
public sealed class SearchController : ControllerBase
{
    private readonly SearchService _searchService;

    public SearchController(SearchService searchService)
    {
        _searchService = searchService;
    }

    [Authorize]
    [HttpPost("results")]
    [ProducesResponseType(typeof(PagedResult<SearchResultItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromBody] SearchResultsRequest request,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfNegative(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var serviceRequest = new global::LimsControlLab.Domain.Services.SearchRequest
        {
            TemplateName = request.TemplateName,
            TestId = request.TestId,
            InstrumentId = request.InstrumentId,
            SampleIdentifier = request.SampleIdentifier,
            FromUtc = request.FromUtc,
            ToUtc = request.ToUtc,
        };

        var searchResults = _searchService.Search(serviceRequest);

        var paged = await searchResults.ToPagedResultAsync(pageNumber, pageSize, ct);

        var dto = new PagedResult<SearchResultItemDto>
        {
            Items = paged.Items.Select(MapToDto).ToList(),
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
        };

        return Ok(dto);
    }

    private static SearchResultItemDto MapToDto(global::LimsControlLab.Domain.Services.SearchResultDto item)
        => new()
        {
            AnalysisId = item.AnalysisId,
            SampleId = item.SampleId,
            SampleIdentifier = item.SampleIdentifier,
            TemplateName = item.TemplateName,
            Site = item.Site,
            Status = item.Status,
            IsLocked = item.IsLocked,
            StartedAtUtc = item.StartedAtUtc,
            CompletedAtUtc = item.CompletedAtUtc,
            ReadingId = item.ReadingId,
            TestId = item.TestId,
            ReadingValue = item.ReadingValue,
            ReadingUnit = item.ReadingUnit,
            CapturedAtUtc = item.CapturedAtUtc,
            ValidationResult = item.ValidationResult,
            CalibratedValue = item.CalibratedValue,
            InstrumentId = item.InstrumentId,
        };
}
