using LimsControlLab.Api.Common;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimsControlLab.Api.Controllers;

[ApiController]
[Route("api/v1/calibration-curves")]
public sealed class CalibrationCurvesController : ControllerBase
{
    private readonly CalibrationCurveService _service;

    public CalibrationCurvesController(CalibrationCurveService service)
    {
        _service = service;
    }

    [Authorize(Policy = "Role.LabCoordinator")]
    [HttpPost]
    [ProducesResponseType(typeof(CalibrationCurveDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(CreateCalibrationCurveRequest request, CancellationToken ct)
    {
        var points = request.Points
            .Select(p => (p.XValue, p.YValue))
            .ToList();

        var result = await _service.CreateAsync(
            request.Name,
            request.AnalysisTemplateId,
            points,
            ct);

        if (result is Outcome<CalibrationCurveDto>.Ok ok)
            return Created(
                $"/api/v1/calibration-curves/{ok.Data.Id}",
                new CalibrationCurveDetailDto
                {
                    Id = ok.Data.Id,
                    Name = ok.Data.Name,
                    AnalysisTemplateId = ok.Data.AnalysisTemplateId,
                    IsActive = ok.Data.IsActive,
                    PointCount = ok.Data.PointCount,
                    RowVersion = ok.Data.RowVersion,
                });

        return result.ToActionResult(this);
    }

    [Authorize(Policy = "Role.LabCoordinator")]
    [HttpGet]
    [ProducesResponseType(typeof(List<CalibrationCurveViewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _service.ListAsync(ct);

        if (result is Outcome<List<CalibrationCurveView>>.Ok ok)
            return Ok(ok.Data.Select(v => new CalibrationCurveViewDto
            {
                Id = v.Id,
                Name = v.Name,
                AnalysisTemplateId = v.AnalysisTemplateId,
                TemplateName = v.TemplateName,
                Site = v.Site,
                IsActive = v.IsActive,
                Points = v.Points
                    .Select(p => new CalibrationPointDto { XValue = p.XValue, YValue = p.YValue })
                    .ToList(),
                RowVersion = v.RowVersion,
            }).ToList());

        return result.ToActionResult(this);
    }

    [Authorize(Policy = "Role.LabCoordinator")]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CalibrationCurveDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);

        if (result is Outcome<CalibrationCurveDto>.Ok ok)
            return Ok(new CalibrationCurveDetailDto
            {
                Id = ok.Data.Id,
                Name = ok.Data.Name,
                AnalysisTemplateId = ok.Data.AnalysisTemplateId,
                IsActive = ok.Data.IsActive,
                PointCount = ok.Data.PointCount,
                RowVersion = ok.Data.RowVersion,
            });

        return result.ToActionResult(this);
    }

    [Authorize(Policy = "Role.LabCoordinator")]
    [HttpPost("{id:int}/deactivate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id, DeactivateCalibrationCurveRequest request, CancellationToken ct)
    {
        var result = await _service.DeactivateAsync(id, ct);

        if (result is Outcome<bool>.Ok)
            return Ok(new { message = "Calibration curve deactivated." });

        return result.ToActionResult(this);
    }
}
