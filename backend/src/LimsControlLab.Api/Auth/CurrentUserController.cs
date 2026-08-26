using LimsControlLab.Domain.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimsControlLab.Api.Auth;

[ApiController]
[Route("api/v1/auth")]
[Authorize]
public sealed class CurrentUserController(ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    public IActionResult GetCurrentUser()
    {
        return Ok(new CurrentUserDto(
            currentUser.UserId,
            currentUser.Username,
            currentUser.Role.ToString(),
            currentUser.Site.ToString()));
    }
}

public sealed record CurrentUserDto(int UserId, string Username, string Role, string Site);
