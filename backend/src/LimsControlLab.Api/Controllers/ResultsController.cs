using LimsControlLab.Api.Common;
using LimsControlLab.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimsControlLab.Api.Controllers;

[ApiController]
[Route("api/v1/results")]
public sealed class ResultsController : ControllerBase
{
    private readonly ResultComparisonService _resultComparisonService;

    public ResultsController(ResultComparisonService resultComparisonService)
    {
        _resultComparisonService = resultComparisonService;
    }

    [Authorize]
    [HttpPost("comparison")]
    [ProducesResponseType(typeof(ResultComparisonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Comparison([FromBody] ResultComparisonRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = new ResultComparisonQuery
        {
            TemplateName = request.TemplateName,
            TestId = request.TestId,
            SampleIdentifier = request.SampleIdentifier,
            FromUtc = request.FromUtc,
            ToUtc = request.ToUtc,
        };

        var result = await _resultComparisonService.CompareAsync(query, ct);

        return result switch
        {
            LimsControlLab.Domain.Common.Outcome<ResultComparisonResult>.Ok ok => Ok(MapToDto(ok.Data)),
            _ => result.ToActionResult(this),
        };
    }

    private static ResultComparisonResponse MapToDto(ResultComparisonResult result)
        => new()
        {
            Unit = result.Unit,
            ToleranceMin = result.ToleranceMin,
            ToleranceMax = result.ToleranceMax,
            TotalPoints = result.TotalPoints,
            Points = result.Points.Select(p => new ResultComparisonPointDto
            {
                AnalysisId = p.AnalysisId,
                SampleId = p.SampleId,
                SampleIdentifier = p.SampleIdentifier,
                TemplateName = p.TemplateName,
                TestId = p.TestId,
                Value = p.Value,
                Unit = p.Unit,
                CapturedAtUtc = p.CapturedAtUtc,
                ValidationResult = p.ValidationResult,
                CalibratedValue = p.CalibratedValue,
            }).ToList(),
        };
}
