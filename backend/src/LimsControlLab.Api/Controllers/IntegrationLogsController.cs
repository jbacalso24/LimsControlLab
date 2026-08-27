using LimsControlLab.Api.Common;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimsControlLab.Api.Controllers;

[ApiController]
[Route("api/v1/integration-logs")]
public sealed class IntegrationLogsController : ControllerBase
{
    private readonly IntegrationMonitoringService _service;

    public IntegrationLogsController(IntegrationMonitoringService service)
    {
        _service = service;
    }

    [Authorize(Policy = "Role.LabCoordinator")]
    [HttpGet]
    [ProducesResponseType(typeof(List<IntegrationLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] string? targetSystem,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(status, targetSystem, ct);

        if (result is Outcome<List<IntegrationLogItem>>.Ok ok)
            return Ok(ok.Data.Select(i => new IntegrationLogDto
            {
                Id = i.Id,
                TargetSystem = i.TargetSystem,
                AnalysisId = i.AnalysisId,
                Status = i.Status,
                AttemptedAtUtc = i.AttemptedAtUtc,
                CompletedAtUtc = i.CompletedAtUtc,
                ErrorMessage = i.ErrorMessage,
                RetryCount = i.RetryCount,
            }).ToList());

        return result.ToActionResult(this);
    }

    [Authorize(Policy = "Role.LabCoordinator")]
    [HttpPost("{id:int}/reprocess")]
    [ProducesResponseType(typeof(ReprocessResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reprocess(int id, CancellationToken ct)
    {
        var result = await _service.ReprocessAsync(id, ct);

        if (result is Outcome<ReprocessResult>.Ok ok)
            return Ok(new ReprocessResultDto
            {
                Id = ok.Data.Id,
                Success = ok.Data.Success,
                Status = ok.Data.Status,
                Message = ok.Data.Message,
            });

        return result.ToActionResult(this);
    }
}
