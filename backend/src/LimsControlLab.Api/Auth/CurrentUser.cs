using System.Security.Claims;
using LimsControlLab.Domain.Auth;
using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Api.Auth;

/// <summary>
/// Reads the current user's identity from the request claims.
/// Populated from JWT token claims; created only in authenticated contexts.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    public int UserId { get; }
    public string Username { get; }
    public Role Role { get; }
    public Site Site { get; }

    public CurrentUser(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            throw new InvalidOperationException("User ID claim not found or invalid.");

        UserId = userId;

        Username = principal.FindFirst(ClaimTypes.Name)?.Value
            ?? throw new InvalidOperationException("Username claim not found.");

        var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;
        if (!Enum.TryParse<Role>(roleClaim, out var role))
            throw new InvalidOperationException("Role claim not found or invalid.");

        Role = role;

        var siteClaim = principal.FindFirst("site")?.Value;
        if (!Enum.TryParse<Site>(siteClaim, out var site))
            throw new InvalidOperationException("Site claim not found or invalid.");

        Site = site;
    }
}
