#pragma warning disable CA1707

using System.IdentityModel.Tokens.Jwt;
using System.Text;
using LimsControlLab.Api;
using LimsControlLab.Api.Auth;
using LimsControlLab.Api.Controllers;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Infrastructure;
using LimsControlLab.SharedKernel.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace LimsControlLab.Api.Tests.Integration;

public sealed class CalibrationCurvesIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _coordinatorClient = null!;
    private HttpClient _analystClient = null!;
    private const string TestDbName = "cane-db-test-calibration";
    private int _curveId;
    private int _templateId;

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
                        services.Remove(descriptor);

                    services.AddDbContext<LimsDbContext>(options =>
                        options.UseSqlServer($"Server=localhost;Database={TestDbName};Trusted_Connection=True;TrustServerCertificate=True;"));
                });
            });

        _coordinatorClient = _factory.CreateClient();
        _analystClient = _factory.CreateClient();

        using var scope2 = _factory.Services.CreateScope();
        var db = scope2.ServiceProvider.GetRequiredService<LimsDbContext>();
        await db.Database.MigrateAsync();

        var passwordHasher = new PasswordHasher();
        var hashedPassword = passwordHasher.HashPassword(null, "TestPassword123!");

        var coordinator = new User
        {
            Username = "coordinator",
            PasswordHash = hashedPassword,
            Role = Role.LabCoordinator,
            Site = Site.Inkerman,
        };

        var analyst = new User
        {
            Username = "analyst",
            PasswordHash = hashedPassword,
            Role = Role.ControlLabAnalyst,
            Site = Site.Inkerman,
        };

        db.Users.AddRange(coordinator, analyst);
        await db.SaveChangesAsync();

        var template = new AnalysisTemplate
        {
            Name = "CalibrationTemplate",
            Site = Site.Inkerman,
            IsRetired = false,
            MinTolerance = 1m,
            MaxTolerance = 5m,
        };

        db.AnalysisTemplates.Add(template);
        await db.SaveChangesAsync();

        _templateId = template.Id;

        _coordinatorClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", CreateToken(coordinator.Id, coordinator.Username, coordinator.Role, coordinator.Site));

        _analystClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", CreateToken(analyst.Id, analyst.Username, analyst.Role, analyst.Site));
    }

    public async Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        await db.Database.EnsureDeletedAsync();
        await _factory.DisposeAsync();
        _coordinatorClient.Dispose();
        _analystClient.Dispose();
    }

    [Fact]
    public async Task CreateCurveByCoordinator_Returns201()
    {
        var request = new CreateCalibrationCurveRequest
        {
            Name = "TestCurve",
            AnalysisTemplateId = _templateId,
            Points = new List<CalibrationPointRequest> { new CalibrationPointRequest { XValue = 0m, YValue = 0m }, new CalibrationPointRequest { XValue = 100m, YValue = 100m } },
        };

        var response = await _coordinatorClient.PostAsJsonAsync("/api/v1/calibration-curves", request);

        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        var curve = await db.CalibrationCurves.FirstOrDefaultAsync();
        Assert.NotNull(curve);
        _curveId = curve.Id;
    }

    [Fact]
    public async Task CreateCurveByAnalyst_Returns403()
    {
        var request = new CreateCalibrationCurveRequest
        {
            Name = "TestCurve",
            AnalysisTemplateId = _templateId,
            Points = new List<CalibrationPointRequest> { new CalibrationPointRequest { XValue = 0m, YValue = 0m }, new CalibrationPointRequest { XValue = 100m, YValue = 100m } },
        };

        var response = await _analystClient.PostAsJsonAsync("/api/v1/calibration-curves", request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetCurveById_Returns200()
    {
        await CreateTestCurve();

        var response = await _coordinatorClient.GetAsync($"/api/v1/calibration-curves/{_curveId}");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateCurveByCoordinator_Returns200()
    {
        await CreateTestCurve();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        var curve = await db.CalibrationCurves.FirstOrDefaultAsync();
        Assert.NotNull(curve);
        var currentRowVersion = Convert.ToBase64String(curve.RowVersion);

        var request = new DeactivateCalibrationCurveRequest { RowVersion = currentRowVersion };

        var response = await _coordinatorClient.PostAsJsonAsync($"/api/v1/calibration-curves/{_curveId}/deactivate", request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateCurveByAnalyst_Returns403()
    {
        await CreateTestCurve();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        var curve = await db.CalibrationCurves.FirstOrDefaultAsync();
        Assert.NotNull(curve);
        var currentRowVersion = Convert.ToBase64String(curve.RowVersion);

        var request = new DeactivateCalibrationCurveRequest { RowVersion = currentRowVersion };

        var response = await _analystClient.PostAsJsonAsync($"/api/v1/calibration-curves/{_curveId}/deactivate", request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task CreateTestCurve()
    {
        var request = new CreateCalibrationCurveRequest
        {
            Name = "TestCurve",
            AnalysisTemplateId = _templateId,
            Points = new List<CalibrationPointRequest> { new CalibrationPointRequest { XValue = 0m, YValue = 0m }, new CalibrationPointRequest { XValue = 100m, YValue = 100m } },
        };

        var response = await _coordinatorClient.PostAsJsonAsync("/api/v1/calibration-curves", request);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        var curve = await db.CalibrationCurves.FirstOrDefaultAsync();
        Assert.NotNull(curve);
        _curveId = curve.Id;
    }

    private static string CreateToken(int userId, string username, Role role, Site site)
    {
        var key = Encoding.UTF8.GetBytes("SuperSecretKeyForDevelopmentThatIsAtLeast32CharactersLongForHS256!!!!");
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role.ToString()),
                new System.Security.Claims.Claim("site", site.ToString()),
            }),
            Issuer = "LimsControlLab",
            Audience = "LimsControlLab",
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256),
        });

        return handler.WriteToken(token);
    }
}
