using LimsControlLab.Api.Common;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimsControlLab.Api.Controllers;

[ApiController]
[Route("api/v1/results")]
public sealed class ResultReviewController : ControllerBase
{
    private readonly ResultLockingService _resultLockingService;
    private readonly AnalysisExecutionService _analysisService;

    public ResultReviewController(ResultLockingService resultLockingService, AnalysisExecutionService analysisService)
    {
        _resultLockingService = resultLockingService;
        _analysisService = analysisService;
    }

    [Authorize(Policy = "Role.LabCoordinator")]
    [HttpGet("exception-analyses")]
    [ProducesResponseType(typeof(List<ResultReviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExceptionResults(CancellationToken ct)
    {
        var result = await _resultLockingService.GetExceptionAnalysesAsync(ct);

        if (result is Outcome<List<ExceptionAnalysisResult>>.Ok ok)
        {
            var dtos = ok.Data.Select(a => new ResultReviewDto
            {
                Id = a.Id,
                SampleId = a.SampleId,
                SampleIdentifier = a.SampleIdentifier,
                TemplateId = a.TemplateId,
                TemplateName = a.TemplateName,
                Site = a.Site,
                Status = a.Status,
                StartedAtUtc = a.StartedAtUtc,
                CompletedAtUtc = a.CompletedAtUtc,
                StartedByUserId = a.StartedByUserId,
                IsLocked = a.IsLocked,
                LockedAtUtc = a.LockedAtUtc,
                LockedByUserId = a.LockedByUserId,
                Exceptions = a.Exceptions.Select(e => new ExceptionDetailDto
                {
                    Id = e.Id,
                    ReadingId = e.ReadingId,
                    Reason = e.Reason,
                    Decision = e.Decision,
                    DecisionComment = e.DecisionComment,
                    DecidedByUserId = e.DecidedByUserId,
                    DecidedAtUtc = e.DecidedAtUtc,
                    RowVersion = e.RowVersion,
                }).ToArray(),
                RowVersion = a.RowVersion,
            }).ToList();

            return Ok(dtos);
        }

        return result.ToActionResult(this);
    }

    [Authorize(Policy = "Role.LabCoordinator")]
    [HttpPatch("{analysisId:int}/unlock")]
    [ProducesResponseType(typeof(UnlockResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UnlockResult(int analysisId, UnlockResultRequest request, CancellationToken ct)
    {
        var result = await _resultLockingService.UnlockResultAsync(
            analysisId,
            new global::LimsControlLab.Domain.Services.UnlockResultRequest
            {
                Justification = request.Justification,
                RowVersion = request.RowVersion,
            },
            ct);

        if (result is Outcome<ResultUnlockResult>.Ok ok)
            return Ok(new UnlockResultDto
            {
                Id = ok.Data.Id,
                IsLocked = ok.Data.IsLocked,
                RowVersion = ok.Data.RowVersion,
            });

        return result.ToActionResult(this);
    }
}
