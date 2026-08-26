using LimsControlLab.Api.Common;
using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Services;
using LimsControlLab.SharedKernel.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimsControlLab.Api.Controllers;

[ApiController]
[Route("api/v1/analysis-templates")]
public sealed class AnalysisTemplatesController : ControllerBase
{
    private readonly AnalysisTemplateService _templateService;
    private readonly ICurrentUser _currentUser;

    public AnalysisTemplatesController(AnalysisTemplateService templateService, ICurrentUser currentUser)
    {
        _templateService = templateService;
        _currentUser = currentUser;
    }

    [Authorize(Policy = "Role.LabCoordinator")]
    [HttpPost]
    [ProducesResponseType(typeof(AnalysisTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateAnalysisTemplateRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<Site>(request.Site, out var site))
            return BadRequest(new { error = "Invalid site value" });

        var serviceRequest = new CreateTemplateRequest
        {
            Name = request.Name,
            Site = site,
            TestConfiguration = request.TestConfiguration,
            CalculationDefinitions = request.CalculationDefinitions,
            ValidationRules = request.ValidationRules,
            MinTolerance = request.MinTolerance,
            MaxTolerance = request.MaxTolerance,
        };

        var result = await _templateService.CreateAsync(serviceRequest, ct);

        if (result is Outcome<AnalysisTemplateServiceDto>.Ok ok)
            return Created($"/api/v1/analysis-templates/{ok.Data.Id}", MapToDto(ok.Data));

        return result.ToActionResult(this);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AnalysisTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _templateService.GetByIdAsync(id, ct);

        if (result is Outcome<AnalysisTemplateServiceDto>.Ok ok)
            return Ok(MapToDto(ok.Data));

        return result.ToActionResult(this);
    }

    [Authorize(Policy = "Role.LabCoordinator")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AnalysisTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, UpdateAnalysisTemplateRequest request, CancellationToken ct)
    {
        var serviceRequest = new UpdateTemplateRequest
        {
            Name = request.Name,
            TestConfiguration = request.TestConfiguration,
            CalculationDefinitions = request.CalculationDefinitions,
            ValidationRules = request.ValidationRules,
            MinTolerance = request.MinTolerance,
            MaxTolerance = request.MaxTolerance,
            RowVersion = Convert.FromBase64String(request.RowVersion),
        };

        var result = await _templateService.UpdateAsync(id, serviceRequest, ct);

        if (result is Outcome<AnalysisTemplateServiceDto>.Ok ok)
            return Ok(MapToDto(ok.Data));

        return result.ToActionResult(this);
    }

    [Authorize(Policy = "Role.LabCoordinator")]
    [HttpPost("{id:int}/retire")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Retire(int id, CancellationToken ct)
    {
        var result = await _templateService.RetireAsync(id, ct);

        if (result is Outcome<bool>.Ok)
            return NoContent();

        return result.ToActionResult(this);
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(List<AnalysisTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _templateService.ListAsync(_currentUser.Site, ct);

        if (result is Outcome<List<AnalysisTemplateServiceDto>>.Ok ok)
            return Ok(ok.Data.Select(MapToDto).ToList());

        return result.ToActionResult(this);
    }

    private static AnalysisTemplateDto MapToDto(AnalysisTemplateServiceDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Site = dto.Site,
        Version = dto.Version,
        IsRetired = dto.IsRetired,
        TestConfiguration = dto.TestConfiguration,
        CalculationDefinitions = dto.CalculationDefinitions,
        ValidationRules = dto.ValidationRules,
        MinTolerance = dto.MinTolerance,
        MaxTolerance = dto.MaxTolerance,
        RowVersion = Convert.ToBase64String(dto.RowVersion),
    };
}
