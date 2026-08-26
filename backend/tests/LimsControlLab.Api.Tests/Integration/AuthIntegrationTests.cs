using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using LimsControlLab.Api;
using LimsControlLab.Api.Auth;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Infrastructure;
using LimsControlLab.SharedKernel.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LimsControlLab.Api.Tests.Integration;

public sealed class AuthIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private const string TestDbName = "cane-db-test-auth";

    public async Task InitializeAsync()
    {
        using (var scope = new ServiceCollection()
            .AddDbContext<LimsDbContext>(options =>
                options.UseSqlServer($"Server=localhost;Database={TestDbName};Trusted_Connection=True;TrustServerCertificate=True;"))
            .BuildServiceProvider()
            .CreateScope())
        {
            var dbTemp = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
            await dbTemp.Database.EnsureDeletedAsync();
        }

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<LimsDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<LimsDbContext>(options =>
                        options.UseSqlServer($"Server=localhost;Database={TestDbName};Trusted_Connection=True;TrustServerCertificate=True;"));

                    services.Configure<JwtOptions>(options =>
                        options.SigningKey = "SuperSecretKeyForDevelopmentThatIsAtLeast32CharactersLongForHS256!!!!");
                });
            });

        _client = _factory.CreateClient();

        using var scope2 = _factory.Services.CreateScope();
        var db = scope2.ServiceProvider.GetRequiredService<LimsDbContext>();

        // Seed test user
        var passwordHasher = new PasswordHasher();
        var hashedPassword = passwordHasher.HashPassword(null, "TestPassword123!");

        var user = new User
        {
            Username = "testanalyst",
            PasswordHash = hashedPassword,
            Role = Role.ControlLabAnalyst,
            Site = Site.Inkerman,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        await db.Database.EnsureDeletedAsync();

        await _factory.DisposeAsync();
        _client.Dispose();
    }

    [Fact]
    public async Task LoginWithValidCredentialsReturns200WithJwtToken()
    {
        var request = new { username = "testanalyst", password = "TestPassword123!" };
        var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/v1/auth/login", content);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);
        var token = doc.RootElement.GetProperty("token").GetString();

        Assert.NotEmpty(token!);
    }

    [Fact]
    public async Task LoginTokenContainsCorrectRoleAndSiteClaims()
    {
        var request = new { username = "testanalyst", password = "TestPassword123!" };
        var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/v1/auth/login", content);
        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);
        var token = doc.RootElement.GetProperty("token").GetString();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

        Assert.NotNull(jwtToken);
        var roleClaim = jwtToken!.Claims.FirstOrDefault(c => c.Type == "role");
        var siteClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "site");

        Assert.NotNull(roleClaim);
        Assert.Equal("ControlLabAnalyst", roleClaim!.Value);
        Assert.NotNull(siteClaim);
        Assert.Equal("Inkerman", siteClaim!.Value);
    }

    [Fact]
    public async Task LoginWithInvalidPasswordReturns401()
    {
        var request = new { username = "testanalyst", password = "WrongPassword" };
        var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/v1/auth/login", content);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizedEndpointWithValidTokenReturns200()
    {
        // Login and get token
        var loginRequest = new { username = "testanalyst", password = "TestPassword123!" };
        var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), System.Text.Encoding.UTF8, "application/json");
        var loginResponse = await _client.PostAsync("/api/v1/auth/login", loginContent);
        var loginBody = await loginResponse.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(loginBody);
        var token = doc.RootElement.GetProperty("token").GetString();

        // Call authorized endpoint with token
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var meBody = await response.Content.ReadAsStringAsync();
        using var meDoc = JsonDocument.Parse(meBody);
        Assert.Equal("1", meDoc.RootElement.GetProperty("userId").ToString());
    }

    [Fact]
    public async Task AuthorizedEndpointWithoutTokenReturns401()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class JwtOptions
    {
        public string SigningKey { get; set; } = "";
    }
}
