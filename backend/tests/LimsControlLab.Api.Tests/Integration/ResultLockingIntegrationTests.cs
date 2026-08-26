#pragma warning disable CA1707

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Globalization;
using System.Net.Http.Json;
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

public sealed class ResultLockingIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _coordinatorClient = null!;
    private HttpClient _analystClient = null!;
    private const string TestDbName = "cane-db-test-locking";
    private int _analysisId;
    private byte[] _originalRowVersion = null!;

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

        var template = new AnalysisTemplate
        {
            Name = "TestTemplate",
            Site = Site.Inkerman,
            IsRetired = false,
            MinTolerance = 1m,
            MaxTolerance = 5m,
        };

        db.AnalysisTemplates.Add(template);
        await db.SaveChangesAsync();

        var version = new AnalysisTemplateVersion
        {
            TemplateId = template.Id,
            Version = 1,
            MinTolerance = 1m,
            MaxTolerance = 5m,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        db.AnalysisTemplateVersions.Add(version);
        await db.SaveChangesAsync();

        template.CurrentVersionId = version.Id;
        await db.SaveChangesAsync();

        var sample = new Sample
        {
            Identifier = "S001",
            AnalysisTemplateId = template.Id,
            Status = LifecycleStatus.NotStarted,
            Site = Site.Inkerman,
            CurrentSite = Site.Inkerman,
        };

        db.Samples.Add(sample);
        await db.SaveChangesAsync();

        var analysis = new Analysis
        {
            SampleId = sample.Id,
            TemplateId = template.Id,
            TemplateVersionId = version.Id,
            Status = LifecycleStatus.Completed,
            StartedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            CompletedAtUtc = DateTimeOffset.UtcNow,
            StartedByUserId = analyst.Id,
            IsLocked = true,
            LockedAtUtc = DateTimeOffset.UtcNow,
            LockedByUserId = coordinator.Id,
        };

        db.Analyses.Add(analysis);
        await db.SaveChangesAsync();

        _analysisId = analysis.Id;
        _originalRowVersion = analysis.RowVersion;

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
    public async Task GetExceptionResults_WithExceptionAnalyses_ReturnsAnalysesWithExceptions()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();

        var template = await db.AnalysisTemplates.FirstAsync();
        var sample = await db.Samples.FirstAsync();

        var analysisWithException = new Analysis
        {
            SampleId = sample.Id,
            TemplateId = template.Id,
            TemplateVersionId = template.CurrentVersionId!.Value,
            Status = LifecycleStatus.Completed,
            StartedAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
            CompletedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            StartedByUserId = 1,
            IsLocked = true,
            LockedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-30),
            LockedByUserId = 2,
        };

        var analysisWithoutException = new Analysis
        {
            SampleId = sample.Id,
            TemplateId = template.Id,
            TemplateVersionId = template.CurrentVersionId!.Value,
            Status = LifecycleStatus.Completed,
            StartedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            CompletedAtUtc = DateTimeOffset.UtcNow,
            StartedByUserId = 1,
            IsLocked = false,
        };

        db.Analyses.AddRange(analysisWithException, analysisWithoutException);
        await db.SaveChangesAsync();

        var reading = new Reading
        {
            AnalysisId = analysisWithException.Id,
            TestId = 1,
            Value = 10m,
            Unit = "mg/L",
            CapturedAtUtc = DateTimeOffset.UtcNow.AddHours(-1).AddMinutes(-30),
            CapturedByUserId = 1,
            ValidationResult = "OutOfTolerance",
        };

        db.Readings.Add(reading);
        await db.SaveChangesAsync();

        var exception = new ExceptionRecord
        {
            AnalysisId = analysisWithException.Id,
            ReadingId = reading.Id,
            Reason = "Reading exceeds maximum tolerance",
            Decision = null,
        };

        db.ExceptionRecords.Add(exception);
        await db.SaveChangesAsync();

        var response = await _coordinatorClient.GetAsync("/api/v1/results/exception-analyses");

        Assert.True(response.IsSuccessStatusCode);
        var content = await response.Content.ReadFromJsonAsync<List<ResultReviewDto>>();

        Assert.NotNull(content);
        Assert.Single(content);
        Assert.Equal(analysisWithException.Id, content![0].Id);
        Assert.NotEmpty(content[0].Exceptions);
        Assert.Equal("Reading exceeds maximum tolerance", content[0].Exceptions[0].Reason);
    }

    [Fact]
    public async Task UnlockResult_WithValidRequest_ReturnsOk()
    {
        var request = new UnlockResultRequest
        {
            Justification = "Override for emergency situation",
            RowVersion = Convert.ToBase64String(_originalRowVersion),
        };

        var response = await _coordinatorClient.PatchAsJsonAsync(
            $"/api/v1/results/{_analysisId}/unlock",
            request);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task UnlockResult_WithAnalystRole_ReturnsForbidden()
    {
        var request = new UnlockResultRequest
        {
            Justification = "Override for emergency situation",
            RowVersion = Convert.ToBase64String(_originalRowVersion),
        };

        var response = await _analystClient.PatchAsJsonAsync(
            $"/api/v1/results/{_analysisId}/unlock",
            request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnlockResult_WithoutJustification_ReturnsBadRequest()
    {
        var request = new UnlockResultRequest
        {
            Justification = string.Empty,
            RowVersion = Convert.ToBase64String(_originalRowVersion),
        };

        var response = await _coordinatorClient.PatchAsJsonAsync(
            $"/api/v1/results/{_analysisId}/unlock",
            request);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnlockResult_WithStaleRowVersion_ReturnsConflict()
    {
        // First unlock should succeed
        var firstRequest = new UnlockResultRequest
        {
            Justification = "First unlock",
            RowVersion = Convert.ToBase64String(_originalRowVersion),
        };

        var firstResponse = await _coordinatorClient.PatchAsJsonAsync(
            $"/api/v1/results/{_analysisId}/unlock",
            firstRequest);

        Assert.True(firstResponse.IsSuccessStatusCode);

        // Get fresh rowVersion from DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        var refreshedAnalysis = await db.Analyses.FirstAsync(a => a.Id == _analysisId);

        // Lock the analysis again manually for the second test
        refreshedAnalysis.IsLocked = true;
        refreshedAnalysis.LockedAtUtc = DateTimeOffset.UtcNow;
        refreshedAnalysis.LockedByUserId = 2;
        await db.SaveChangesAsync();

        // Second unlock with stale rowVersion should return 409 Conflict
        var secondRequest = new UnlockResultRequest
        {
            Justification = "Second unlock attempt",
            RowVersion = Convert.ToBase64String(_originalRowVersion),
        };

        var secondResponse = await _coordinatorClient.PatchAsJsonAsync(
            $"/api/v1/results/{_analysisId}/unlock",
            secondRequest);

        Assert.Equal(System.Net.HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    private static string CreateToken(int userId, string username, Role role, Site site)
    {
        var key = Encoding.UTF8.GetBytes("SuperSecretKeyForDevelopmentThatIsAtLeast32CharactersLongForHS256!!!!");
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture)),
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
