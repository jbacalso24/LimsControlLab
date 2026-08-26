using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LimsControlLab.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace LimsControlLab.Api.Auth;

/// <summary>
/// Issues JWT tokens bearing user role and site claims.
/// Tokens are signed but not encrypted; claims are readable but tamper-evident.
/// </summary>
public sealed class JwtTokenService
{
    private const string Issuer = "LimsControlLab";
    private const string Audience = "LimsControlLab";
    private readonly string _jwtSecret;
    private readonly TimeProvider _clock;

    public JwtTokenService(IConfiguration config, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(clock);

        _jwtSecret = config["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey not configured");
        _clock = clock;
    }

    public string CreateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("role", user.Role.ToString()),
            new Claim("site", user.Site.ToString()),
        };

        var now = _clock.GetUtcNow().UtcDateTime;
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: now.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
