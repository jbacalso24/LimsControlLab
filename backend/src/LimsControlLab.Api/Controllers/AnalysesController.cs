using LimsControlLab.Api.Common;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimsControlLab.Api.Controllers;

[ApiController]
[Route("api/v1/analyses")]
public sealed class AnalysesController : ControllerBase
{
    private readonly AnalysisExecutionService _analysisService;

    public AnalysesController(AnalysisExecutionService analysisService)
    {
        _analysisService = analysisService;
    }

    [Authorize]
    [HttpGet("{analysisId:int}")]
    [ProducesResponseType(typeof(AnalysisDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalysisDetail(int analysisId, CancellationToken ct)
    {
        var result = await _analysisService.GetAnalysisDetailAsync(analysisId, ct);

        if (result is Outcome<AnalysisDetailResult>.Ok ok)
        {
            var readingDtos = ok.Data.Readings.Select(r => new ReadingDto
            {
                Id = r.Id,
                TestId = r.TestId,
                Value = r.Value,
                Unit = r.Unit,
                CapturedAtUtc = r.CapturedAtUtc,
                CapturedBy = r.CapturedByUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                CapturedByUsername = r.CapturedByUsername,
                ValidationResult = new ReadingValidationDto
                {
                    IsValid = r.ValidationResult.IsValid,
                    ExpectedRange = r.ValidationResult.ExpectedRange,
                    ActualValue = r.ValidationResult.ActualValue,
                    Reason = r.ValidationResult.Reason,
                },
                CalibratedValue = r.CalibratedValue,
            }).ToList();

            var exceptionDtos = ok.Data.Exceptions.Select(e => new ExceptionDto
            {
                Id = e.Id,
                ReadingId = e.ReadingId,
                Reason = e.Reason,
                Decision = e.Decision,
                DecisionComment = e.DecisionComment,
                RowVersion = e.RowVersion,
            }).ToList();

            var availableTestDtos = ok.Data.AvailableTests.Select(t => new TestDefinitionDto
            {
                Id = t.Id,
                Name = t.Name,
                Unit = t.Unit,
                Method = t.Method,
            }).ToList();

            return Ok(new AnalysisDetailDto
            {
                Id = ok.Data.Id,
                SampleId = ok.Data.SampleId,
                TemplateId = ok.Data.TemplateId,
                Status = ok.Data.Status,
                IsLocked = ok.Data.IsLocked,
                AvailableTests = availableTestDtos,
                Readings = readingDtos,
                Exceptions = exceptionDtos,
                RowVersion = ok.Data.RowVersion,
            });
        }

        return result.ToActionResult(this);
    }

    [Authorize(Policy = "Role.ControlLabAnalyst")]
    [HttpPost("{analysisId:int}/readings")]
    [ProducesResponseType(typeof(ReadingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CaptureReading(int analysisId, CreateReadingRequest request, CancellationToken ct)
    {
        var serviceRequest = new global::LimsControlLab.Domain.Services.CaptureReadingRequest
        {
            TestId = request.TestId,
            Value = request.Value,
            Unit = request.Unit,
            CapturedAtUtc = request.CapturedAtUtc,
            InstrumentId = request.InstrumentId,
        };

        var result = await _analysisService.CaptureReadingAsync(analysisId, serviceRequest, ct);

        if (result is Outcome<ReadingCaptureResult>.Ok ok)
            return Created(
                $"/api/v1/analyses/{analysisId}/readings/{ok.Data.Id}",
                new ReadingDto
                {
                    Id = ok.Data.Id,
                    TestId = ok.Data.TestId,
                    Value = ok.Data.Value,
                    Unit = ok.Data.Unit,
                    CapturedAtUtc = ok.Data.CapturedAtUtc,
                    CapturedBy = ok.Data.CapturedByUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    CapturedByUsername = ok.Data.CapturedByUsername,
                    ValidationResult = new ReadingValidationDto
                    {
                        IsValid = ok.Data.ValidationResult.IsValid,
                        ExpectedRange = ok.Data.ValidationResult.ExpectedRange,
                        ActualValue = ok.Data.ValidationResult.ActualValue,
                        Reason = ok.Data.ValidationResult.Reason,
                    },
                    CalibratedValue = ok.Data.CalibratedValue,
                });

        return result.ToActionResult(this);
    }

    [Authorize(Policy = "Role.LabCoordinator")]
    [HttpPost("{analysisId:int}/exceptions/{exceptionId:int}/decision")]
    [ProducesResponseType(typeof(ExceptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DecideException(int analysisId, int exceptionId, ExceptionDecisionRequest request, CancellationToken ct)
    {
        var serviceRequest = new global::LimsControlLab.Domain.Services.ExceptionDecisionRequest
        {
            Decision = request.Decision,
            Comment = request.Comment,
            RowVersion = request.RowVersion,
        };

        var result = await _analysisService.DecideExceptionAsync(analysisId, exceptionId, serviceRequest, ct);

        if (result is Outcome<ExceptionDecisionResult>.Ok ok)
            return Ok(new ExceptionDto
            {
                Id = ok.Data.Id,
                ReadingId = ok.Data.ReadingId,
                Reason = ok.Data.Reason,
                Decision = ok.Data.Decision,
                DecisionComment = ok.Data.DecisionComment,
                RowVersion = ok.Data.RowVersion,
            });

        return result.ToActionResult(this);
    }

    [Authorize(Policy = "Role.ControlLabAnalyst")]
    [HttpPatch("{analysisId:int}/status")]
    [ProducesResponseType(typeof(AnalysisStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatus(int analysisId, StatusChangeRequest request, CancellationToken ct)
    {
        var serviceRequest = new global::LimsControlLab.Domain.Services.StatusChangeRequest
        {
            Action = request.Action,
            RowVersion = request.RowVersion,
        };

        var result = await _analysisService.ChangeStatusAsync(analysisId, serviceRequest, ct);

        if (result is Outcome<AnalysisStatusChangeResult>.Ok ok)
            return Ok(new AnalysisStatusDto
            {
                Id = ok.Data.Id,
                Status = ok.Data.Status,
                IsLocked = ok.Data.IsLocked,
                RowVersion = ok.Data.RowVersion,
            });

        return result.ToActionResult(this);
    }
}
