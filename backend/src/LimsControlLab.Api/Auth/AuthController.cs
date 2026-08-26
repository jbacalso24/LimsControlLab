using LimsControlLab.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LimsControlLab.Api.Auth;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    LimsDbContext db,
    JwtTokenService jwtService,
    PasswordHasher passwordHasher) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Username and password are required." });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username, ct);
        if (user is null)
            return Unauthorized(new { error = "Invalid credentials." });

        var result = passwordHasher.VerifyHashedPassword(null, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { error = "Invalid credentials." });

        var token = jwtService.CreateToken(user);
        return Ok(new LoginResponseDto(token, user.Id, user.Username, user.Role.ToString(), user.Site.ToString()));
    }
}

public sealed record LoginRequest(string? Username, string? Password);

// Typed so OpenAPI can actually document the login response shape (a prior
// anonymous-object return made the emitted spec — and therefore the generated
// frontend client — describe this endpoint's response as void).
public sealed record LoginResponseDto(string Token, int UserId, string Username, string Role, string Site);
