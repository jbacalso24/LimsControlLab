using LimsControlLab.Api.Common;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimsControlLab.Api.Controllers;

[ApiController]
[Route("api/v1/audit-logs")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly AuditTrailService _service;

    public AuditLogsController(AuditTrailService service)
    {
        _service = service;
    }

    [Authorize(Policy = "Role.LabCoordinator")]
    [HttpGet]
    [ProducesResponseType(typeof(AuditLogPageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? entityType,
        [FromQuery] string? action,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(entityType, action, page, pageSize, ct);

        if (result is Outcome<AuditLogPageResult>.Ok ok)
            return Ok(new AuditLogPageDto
            {
                Items = ok.Data.Items.Select(i => new AuditLogDto
                {
                    Id = i.Id,
                    UserId = i.UserId,
                    Username = i.Username,
                    Role = i.Role,
                    TimestampUtc = i.TimestampUtc,
                    Action = i.Action,
                    EntityType = i.EntityType,
                    EntityId = i.EntityId,
                    BeforeValues = i.BeforeValues,
                    AfterValues = i.AfterValues,
                    CorrelationId = i.CorrelationId,
                }).ToList(),
                Total = ok.Data.Total,
                Page = ok.Data.Page,
                PageSize = ok.Data.PageSize,
            });

        return result.ToActionResult(this);
    }
}
