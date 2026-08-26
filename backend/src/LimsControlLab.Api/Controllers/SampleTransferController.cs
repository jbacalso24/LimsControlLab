using LimsControlLab.Api.Common;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimsControlLab.Api.Controllers;

[ApiController]
[Route("api/v1/samples")]
public sealed class SampleTransferController : ControllerBase
{
    private readonly SampleTransferService _transferService;

    public SampleTransferController(SampleTransferService transferService)
    {
        _transferService = transferService;
    }

    [Authorize]
    [HttpGet("{sampleId:int}")]
    [ProducesResponseType(typeof(SampleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSample(int sampleId, CancellationToken ct)
    {
        var result = await _transferService.GetByIdAsync(sampleId, ct);

        if (result is Outcome<Sample>.Ok ok)
            return Ok(new SampleDto
            {
                Id = ok.Data.Id,
                Identifier = ok.Data.Identifier,
                Site = ok.Data.Site.ToString(),
                CurrentSite = ok.Data.CurrentSite.ToString(),
                AnalysisTemplateId = ok.Data.AnalysisTemplateId,
                Status = ok.Data.Status.ToString(),
                RowVersion = ok.Data.RowVersion,
            });

        return result.ToActionResult(this);
    }

    [Authorize(Policy = "Role.ControlLabAnalyst")]
    [HttpPost("{sampleId:int}/transfer")]
    [ProducesResponseType(typeof(SampleTransferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Transfer(int sampleId, TransferSampleRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<global::LimsControlLab.SharedKernel.Enums.Site>(request.ToSite, true, out var toSite))
            return BadRequest(new { error = "Invalid site name." });

        var result = await _transferService.TransferAsync(sampleId, toSite, request.RowVersion, ct);

        if (result is Outcome<SampleTransferResult>.Ok ok)
            return Ok(new SampleTransferDto
            {
                Id = ok.Data.Id,
                FromSite = ok.Data.FromSite.ToString(),
                ToSite = ok.Data.ToSite.ToString(),
                TransferredAtUtc = ok.Data.TransferredAtUtc,
                RowVersion = ok.Data.RowVersion,
            });

        return result.ToActionResult(this);
    }
}
