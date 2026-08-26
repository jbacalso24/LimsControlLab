using System.IdentityModel.Tokens.Jwt;
using LimsControlLab.Api.Auth;
using LimsControlLab.Domain.Entities;
using LimsControlLab.SharedKernel.Enums;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LimsControlLab.Api.Tests.Auth;

public sealed class JwtTokenServiceTests
{
    private static JwtTokenService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:SigningKey", "SuperSecretKeyForDevelopmentThatIsAtLeast32CharactersLongForHS256!!!!" }
            })
            .Build();

        return new JwtTokenService(config, TimeProvider.System);
    }

    [Fact]
    public void CreateTokenIssuesTokenWithValidStructure()
    {
        var service = CreateService();
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            PasswordHash = "hash",
            Role = Role.ControlLabAnalyst,
            Site = Site.Inkerman,
        };

        var token = service.CreateToken(user);

        Assert.NotEmpty(token);
        Assert.Contains(".", token);

        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    [Fact]
    public void CreateTokenContainsUserIdClaim()
    {
        var service = CreateService();
        var user = new User
        {
            Id = 42,
            Username = "testuser",
            PasswordHash = "hash",
            Role = Role.LabCoordinator,
            Site = Site.Macknade,
        };

        var token = service.CreateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

        Assert.NotNull(jwtToken);
        var userIdClaim = jwtToken!.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        Assert.NotNull(userIdClaim);
        Assert.Equal("42", userIdClaim!.Value);
    }

    [Fact]
    public void CreateTokenContainsRoleAndSiteClaims()
    {
        var service = CreateService();
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            PasswordHash = "hash",
            Role = Role.ControlLabAnalyst,
            Site = Site.Victoria,
        };

        var token = service.CreateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

        Assert.NotNull(jwtToken);
        var roleClaim = jwtToken!.Claims.FirstOrDefault(c => c.Type == "role");
        var siteClaim = jwtToken!.Claims.FirstOrDefault(c => c.Type == "site");

        Assert.NotNull(roleClaim);
        Assert.Equal("ControlLabAnalyst", roleClaim!.Value);
        Assert.NotNull(siteClaim);
        Assert.Equal("Victoria", siteClaim!.Value);
    }

    [Fact]
    public void CreateTokenHasExpiry()
    {
        var service = CreateService();
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            PasswordHash = "hash",
            Role = Role.ControlLabAnalyst,
            Site = Site.Inkerman,
        };

        var token = service.CreateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

        Assert.NotNull(jwtToken);
        Assert.True(jwtToken!.ValidTo > DateTime.UtcNow);
        Assert.True((jwtToken.ValidTo - DateTime.UtcNow).TotalHours <= 8);
    }
}
