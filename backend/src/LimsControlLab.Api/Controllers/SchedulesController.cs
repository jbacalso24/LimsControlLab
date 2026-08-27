using LimsControlLab.Api.Common;
using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Services;
using LimsControlLab.SharedKernel.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimsControlLab.Api.Controllers;

[ApiController]
[Route("api/v1/schedules")]
public sealed class SchedulesController : ControllerBase
{
    private readonly ScheduleService _scheduleService;
    private readonly ScheduleAdherenceService _scheduleAdherenceService;
    private readonly ICurrentUser _currentUser;

    public SchedulesController(
        ScheduleService scheduleService,
        ScheduleAdherenceService scheduleAdherenceService,
        ICurrentUser currentUser)
    {
        _scheduleService = scheduleService;
        _scheduleAdherenceService = scheduleAdherenceService;
        _currentUser = currentUser;
    }

    [Authorize(Policy = "Role.LabCoordinator")]
    [HttpPost]
    [ProducesResponseType(typeof(ScheduleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateScheduleRequest request, CancellationToken ct)
    {
        var serviceRequest = new global::LimsControlLab.Domain.Services.CreateScheduleRequest
        {
            Site = Enum.Parse<Site>(request.Site),
            Name = request.Name,
            AnalysisType = request.AnalysisType,
            ShiftPattern = Enum.Parse<ShiftPattern>(request.ShiftPattern),
            RecurrencePattern = request.RecurrencePattern,
            ExclusionRules = request.ExclusionRules,
            AssignedToUserId = request.AssignedToUserId,
        };

        var result = await _scheduleService.CreateAsync(serviceRequest, ct);

        if (result is Outcome<global::LimsControlLab.Domain.Services.ScheduleServiceDto>.Ok ok)
            return Created($"/api/v1/schedules/{ok.Data.Id}", MapToDto(ok.Data));

        return result.ToActionResult(this);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _scheduleService.GetByIdAsync(id, ct);

        if (result is Outcome<global::LimsControlLab.Domain.Services.ScheduleServiceDto>.Ok ok)
            return Ok(MapToDto(ok.Data));

        return result.ToActionResult(this);
    }

    [Authorize(Policy = "Role.LabCoordinator")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, UpdateScheduleRequest request, CancellationToken ct)
    {
        var serviceRequest = new global::LimsControlLab.Domain.Services.UpdateScheduleRequest
        {
            Name = request.Name,
            AnalysisType = request.AnalysisType,
            ShiftPattern = Enum.Parse<ShiftPattern>(request.ShiftPattern),
            RecurrencePattern = request.RecurrencePattern,
            ExclusionRules = request.ExclusionRules,
            AssignedToUserId = request.AssignedToUserId,
            IsActive = request.IsActive,
            RowVersion = Convert.FromBase64String(request.RowVersion),
        };

        var result = await _scheduleService.UpdateAsync(id, serviceRequest, ct);

        if (result is Outcome<global::LimsControlLab.Domain.Services.ScheduleServiceDto>.Ok ok)
            return Ok(MapToDto(ok.Data));

        return result.ToActionResult(this);
    }

    [Authorize(Policy = "Role.LabCoordinator")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _scheduleService.DeleteAsync(id, ct);

        if (result is Outcome<bool>.Ok)
            return NoContent();

        return result.ToActionResult(this);
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(List<ScheduleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _scheduleService.ListAsync(_currentUser.Site, ct);

        if (result is Outcome<List<global::LimsControlLab.Domain.Services.ScheduleServiceDto>>.Ok ok)
            return Ok(ok.Data.Select(MapToDto).ToList());

        return result.ToActionResult(this);
    }

    [Authorize]
    [HttpGet("adherence")]
    [ProducesResponseType(typeof(ScheduleAdherenceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdherence(CancellationToken ct)
    {
        var result = await _scheduleAdherenceService.GetAdherenceAsync(_currentUser.Site, ct);

        if (result is Outcome<global::LimsControlLab.Domain.Services.ScheduleAdherenceResult>.Ok ok)
            return Ok(MapToDto(ok.Data));

        return result.ToActionResult(this);
    }

    [Authorize(Policy = "Role.LabCoordinator")]
    [HttpPost("{id:int}/assign")]
    [ProducesResponseType(typeof(ScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(int id, [FromBody] AssignScheduleRequest request, CancellationToken ct)
    {
        var result = await _scheduleService.AssignAsync(id, request.UserId, ct);

        if (result is Outcome<global::LimsControlLab.Domain.Services.ScheduleServiceDto>.Ok ok)
            return Ok(MapToDto(ok.Data));

        return result.ToActionResult(this);
    }

    private static ScheduleDto MapToDto(global::LimsControlLab.Domain.Services.ScheduleServiceDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Site = dto.Site,
        AnalysisType = dto.AnalysisType,
        ShiftPattern = dto.ShiftPattern,
        RecurrencePattern = dto.RecurrencePattern,
        ExclusionRules = dto.ExclusionRules,
        AssignedToUserId = dto.AssignedToUserId,
        IsActive = dto.IsActive,
        RowVersion = Convert.ToBase64String(dto.RowVersion),
    };

    private static ScheduleAdherenceResponse MapToDto(global::LimsControlLab.Domain.Services.ScheduleAdherenceResult result) => new()
    {
        AsOfUtc = result.AsOfUtc,
        Summary = new AdherenceSummaryDto
        {
            OnTrack = result.Summary.OnTrack,
            Due = result.Summary.Due,
            Overdue = result.Summary.Overdue,
            Missed = result.Summary.Missed,
            Total = result.Summary.Total,
        },
        Schedules = result.Schedules.Select(s => new ScheduleAdherenceItemDto
        {
            ScheduleId = s.ScheduleId,
            Name = s.Name,
            AnalysisType = s.AnalysisType,
            ShiftPattern = s.ShiftPattern,
            CadenceLabel = s.CadenceLabel,
            Status = s.Status,
            AssignedToUserId = s.AssignedToUserId,
            AssignedToUsername = s.AssignedToUsername,
            LastAnalysisAtUtc = s.LastAnalysisAtUtc,
            MissedPeriods = s.MissedPeriods,
            CurrentPeriodStartUtc = s.CurrentPeriodStartUtc,
            CurrentPeriodEndUtc = s.CurrentPeriodEndUtc,
        }).ToList(),
    };
}

public sealed record AssignScheduleRequest
{
    public required int UserId { get; init; }
}
