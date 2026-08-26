using LimsControlLab.Api.Common;
using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimsControlLab.Api.Controllers;

[ApiController]
[Route("api/v1/instruments")]
public sealed class InstrumentsController : ControllerBase
{
    private readonly InstrumentReadingService _service;
    private readonly ICurrentUser _currentUser;

    public InstrumentsController(InstrumentReadingService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [Authorize(Policy = "Role.ControlLabAnalyst")]
    [HttpGet]
    [ProducesResponseType(typeof(List<global::LimsControlLab.Domain.Services.InstrumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _service.ListByCurrentSiteAsync(ct);

        if (result is Outcome<List<global::LimsControlLab.Domain.Services.InstrumentDto>>.Ok ok)
            return Ok(ok.Data);

        return result.ToActionResult(this);
    }

    [Authorize(Policy = "Role.ControlLabAnalyst")]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(global::LimsControlLab.Domain.Services.InstrumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);

        if (result is Outcome<global::LimsControlLab.Domain.Services.InstrumentDto>.Ok ok)
            return Ok(ok.Data);

        return result.ToActionResult(this);
    }

    [Authorize(Policy = "Role.LabCoordinator")]
    [HttpPost]
    [ProducesResponseType(typeof(global::LimsControlLab.Domain.Services.InstrumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(global::LimsControlLab.Domain.Services.CreateInstrumentRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);

        if (result is Outcome<global::LimsControlLab.Domain.Services.InstrumentDto>.Ok ok)
            return Created($"/api/v1/instruments/{ok.Data.Id}", ok.Data);

        return result.ToActionResult(this);
    }

    [Authorize(Policy = "Role.LabCoordinator")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(global::LimsControlLab.Domain.Services.InstrumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, global::LimsControlLab.Domain.Services.UpdateInstrumentRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, request, ct);

        if (result is Outcome<global::LimsControlLab.Domain.Services.InstrumentDto>.Ok ok)
            return Ok(ok.Data);

        return result.ToActionResult(this);
    }
}
