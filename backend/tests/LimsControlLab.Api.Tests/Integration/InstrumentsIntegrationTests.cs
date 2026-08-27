#pragma warning disable CA1707

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LimsControlLab.Api;
using LimsControlLab.Api.Auth;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Services;
using LimsControlLab.Infrastructure;
using LimsControlLab.SharedKernel.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Text.Json;
using Xunit;

namespace LimsControlLab.Api.Tests.Integration;

public sealed class InstrumentsIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _analystClient = null!;
    private HttpClient _coordinatorClient = null!;
    private const string TestDbName = "cane-db-test-instruments";
    private int _instrumentId;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task InitializeAsync()
    {
        using (var scope = new ServiceCollection()
            .AddDbContext<LimsDbContext>(options =>
                options.UseNpgsql($"Host=localhost;Port=5432;Database={TestDbName};Username=lims;Password=lims_dev_pw"))
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
                        services.Remove(descriptor);

                    services.AddDbContext<LimsDbContext>(options =>
                        options.UseNpgsql($"Host=localhost;Port=5432;Database={TestDbName};Username=lims;Password=lims_dev_pw"));
                });
            });

        _analystClient = _factory.CreateClient();
        _coordinatorClient = _factory.CreateClient();

        using var scope2 = _factory.Services.CreateScope();
        var db = scope2.ServiceProvider.GetRequiredService<LimsDbContext>();

        var passwordHasher = new PasswordHasher();
        var hashedPassword = passwordHasher.HashPassword(null, "TestPassword123!");

        var analyst = new User
        {
            Username = "analyst",
            PasswordHash = hashedPassword,
            Role = Role.ControlLabAnalyst,
            Site = Site.Inkerman,
        };

        var coordinator = new User
        {
            Username = "coordinator",
            PasswordHash = hashedPassword,
            Role = Role.LabCoordinator,
            Site = Site.Inkerman,
        };

        db.Users.AddRange(analyst, coordinator);
        await db.SaveChangesAsync();

        var instrument = new Instrument
        {
            Name = "Polarimeter-1",
            Model = "APL-330",
            SerialNumber = "SN-12345",
            Site = Site.Inkerman,
            IsActive = true,
        };

        db.Instruments.Add(instrument);
        await db.SaveChangesAsync();
        _instrumentId = instrument.Id;

        var analystToken = CreateToken(analyst.Id, analyst.Username, analyst.Role, analyst.Site);
        var coordinatorToken = CreateToken(coordinator.Id, coordinator.Username, coordinator.Role, coordinator.Site);

        _analystClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", analystToken);
        _coordinatorClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", coordinatorToken);
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();
    }

    [Fact]
    public async Task ListInstruments_WithAnalystRole_ReturnsInstrumentsForUserSite()
    {
        var response = await _analystClient.GetAsync("/api/v1/instruments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var instruments = JsonSerializer.Deserialize<List<InstrumentDto>>(content, JsonOptions);

        Assert.NotNull(instruments);
        Assert.Single(instruments);
        Assert.Equal("Polarimeter-1", instruments[0].Name);
    }

    [Fact]
    public async Task GetInstrumentById_WithValidId_ReturnsInstrument()
    {
        var response = await _analystClient.GetAsync($"/api/v1/instruments/{_instrumentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var instrument = JsonSerializer.Deserialize<InstrumentDto>(content, JsonOptions);

        Assert.NotNull(instrument);
        Assert.Equal("Polarimeter-1", instrument.Name);
        Assert.True(instrument.IsActive);
    }

    [Fact]
    public async Task CreateInstrument_WithCoordinatorRole_ReturnsCreated()
    {
        var request = new CreateInstrumentRequest
        {
            Name = "HPLC-1",
            Model = "Agilent 1100",
            SerialNumber = "SN-67890",
            IsActive = true,
        };

        var response = await _coordinatorClient.PostAsJsonAsync("/api/v1/instruments", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("/api/v1/instruments/", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task CreateInstrument_WithAnalystRole_ReturnsForbidden()
    {
        var request = new CreateInstrumentRequest
        {
            Name = "HPLC-1",
            Model = "Agilent 1100",
            SerialNumber = "SN-67890",
            IsActive = true,
        };

        var response = await _analystClient.PostAsJsonAsync("/api/v1/instruments", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateInstrument_WithAnalystRole_ReturnsForbidden()
    {
        var request = new UpdateInstrumentRequest
        {
            Name = "UpdatedName",
            Model = "UpdatedModel",
            SerialNumber = "SN-12345",
            IsActive = true,
            RowVersion = "dGVzdA==",
        };

        var response = await _analystClient.PutAsJsonAsync($"/api/v1/instruments/{_instrumentId}", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static string CreateToken(int userId, string username, Role role, Site site)
    {
        var key = Encoding.UTF8.GetBytes("SuperSecretKeyForDevelopmentThatIsAtLeast32CharactersLongForHS256!!!!");
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role.ToString()),
                new Claim("site", site.ToString()),
            }),
            Issuer = "LimsControlLab",
            Audience = "LimsControlLab",
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256),
        });

        return handler.WriteToken(token);
    }
}
