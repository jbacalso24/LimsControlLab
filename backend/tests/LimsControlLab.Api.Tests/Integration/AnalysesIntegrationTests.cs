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

public sealed class AnalysesIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _analystClient = null!;
    private HttpClient _coordinatorClient = null!;
    private const string TestDbName = "cane-db-test-analyses";
    private int _analysisId;
    private int _exceptionId;

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

        _analystClient = _factory.CreateClient();
        _coordinatorClient = _factory.CreateClient();

        using var scope2 = _factory.Services.CreateScope();
        var db = scope2.ServiceProvider.GetRequiredService<LimsDbContext>();
        await db.Database.MigrateAsync();

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
            Status = LifecycleStatus.NotStarted,
            StartedAtUtc = DateTimeOffset.UtcNow,
            StartedByUserId = analyst.Id,
            IsLocked = false,
        };

        db.Analyses.Add(analysis);
        await db.SaveChangesAsync();

        _analysisId = analysis.Id;

        _analystClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", CreateToken(analyst.Id, analyst.Username, analyst.Role, analyst.Site));

        _coordinatorClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", CreateToken(coordinator.Id, coordinator.Username, coordinator.Role, coordinator.Site));
    }

    public async Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        await db.Database.EnsureDeletedAsync();
        await _factory.DisposeAsync();
        _analystClient.Dispose();
        _coordinatorClient.Dispose();
    }

    [Fact]
    public async Task CaptureReadingWithValidReading_Returns201()
    {
        var request = new CreateReadingRequest
        {
            TestId = 1,
            Value = 3m,
            Unit = "mg",
            CapturedAtUtc = DateTimeOffset.UtcNow,
        };

        var response = await _analystClient.PostAsJsonAsync($"/api/v1/analyses/{_analysisId}/readings", request);

        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CaptureReadingOutOfTolerance_CreatesException()
    {
        var request = new CreateReadingRequest
        {
            TestId = 1,
            Value = 10m,
            Unit = "mg",
            CapturedAtUtc = DateTimeOffset.UtcNow,
        };

        var response = await _analystClient.PostAsJsonAsync($"/api/v1/analyses/{_analysisId}/readings", request);

        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        var exception = await db.ExceptionRecords.FirstOrDefaultAsync();
        Assert.NotNull(exception);
        _exceptionId = exception.Id;
    }

    [Fact]
    public async Task DecideExceptionByCoordinator_Returns200()
    {
        await CaptureReadingOutOfTolerance_CreatesException();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        var exception = await db.ExceptionRecords.FirstOrDefaultAsync(e => e.Id == _exceptionId);
        var currentRowVersion = Convert.ToBase64String(exception!.RowVersion);

        var request = new ExceptionDecisionRequest
        {
            Decision = "Modify",
            Comment = "Acceptable",
            RowVersion = currentRowVersion,
        };

        var response = await _coordinatorClient.PostAsJsonAsync(
            $"/api/v1/analyses/{_analysisId}/exceptions/{_exceptionId}/decision", request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DecideException_WithComment_RoundTripsDecisionComment()
    {
        await CaptureReadingOutOfTolerance_CreatesException();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        var exception = await db.ExceptionRecords.FirstOrDefaultAsync(e => e.Id == _exceptionId);
        var currentRowVersion = Convert.ToBase64String(exception!.RowVersion);

        var testComment = "Out of tolerance but acceptable after review";
        var request = new ExceptionDecisionRequest
        {
            Decision = "Modify",
            Comment = testComment,
            RowVersion = currentRowVersion,
        };

        var decisionResponse = await _coordinatorClient.PostAsJsonAsync(
            $"/api/v1/analyses/{_analysisId}/exceptions/{_exceptionId}/decision", request);

        Assert.Equal(System.Net.HttpStatusCode.OK, decisionResponse.StatusCode);

        var decisionContent = await decisionResponse.Content.ReadFromJsonAsync<ExceptionDto>();
        Assert.NotNull(decisionContent);
        Assert.Equal(testComment, decisionContent.DecisionComment);

        var getResponse = await _analystClient.GetAsync($"/api/v1/analyses/{_analysisId}");
        Assert.Equal(System.Net.HttpStatusCode.OK, getResponse.StatusCode);

        var analysisContent = await getResponse.Content.ReadFromJsonAsync<AnalysisDetailDto>();
        Assert.NotNull(analysisContent);
        Assert.NotEmpty(analysisContent.Exceptions);

        var resolvedException = analysisContent.Exceptions.FirstOrDefault(e => e.Id == _exceptionId);
        Assert.NotNull(resolvedException);
        Assert.Equal(testComment, resolvedException.DecisionComment);
    }

    [Fact]
    public async Task DecideExceptionByAnalyst_Returns403()
    {
        await CaptureReadingOutOfTolerance_CreatesException();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        var exception = await db.ExceptionRecords.FirstOrDefaultAsync(e => e.Id == _exceptionId);
        var currentRowVersion = Convert.ToBase64String(exception!.RowVersion);

        var request = new ExceptionDecisionRequest
        {
            Decision = "Modify",
            Comment = "Acceptable",
            RowVersion = currentRowVersion,
        };

        var response = await _analystClient.PostAsJsonAsync(
            $"/api/v1/analyses/{_analysisId}/exceptions/{_exceptionId}/decision", request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatusWithValidAction_Returns200()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        var analysis = await db.Analyses.FirstOrDefaultAsync(a => a.Id == _analysisId);
        var currentRowVersion = Convert.ToBase64String(analysis!.RowVersion);

        var request = new StatusChangeRequest { Action = "Start", RowVersion = currentRowVersion };

        var response = await _analystClient.PatchAsJsonAsync($"/api/v1/analyses/{_analysisId}/status", request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAnalysisDetail_WhenExists_Returns200WithCorrectShape()
    {
        var response = await _analystClient.GetAsync($"/api/v1/analyses/{_analysisId}");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<AnalysisDetailDto>();
        Assert.NotNull(content);
        Assert.Equal(_analysisId, content.Id);
        Assert.True(content.SampleId > 0);
        Assert.True(content.TemplateId > 0);
        Assert.NotEmpty(content.Status);
        Assert.NotNull(content.Readings);
        Assert.NotNull(content.Exceptions);
        Assert.NotEmpty(content.RowVersion);
    }

    [Fact]
    public async Task GetAnalysisDetail_WhenNotExists_Returns404()
    {
        var response = await _analystClient.GetAsync("/api/v1/analyses/99999");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAnalysisDetail_ByCoordinator_Returns200()
    {
        var response = await _coordinatorClient.GetAsync($"/api/v1/analyses/{_analysisId}");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<AnalysisDetailDto>();
        Assert.NotNull(content);
        Assert.Equal(_analysisId, content.Id);
    }

    [Fact]
    public async Task CompleteAnalysis_LocksItAndAllowsUnlock()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        var analysis = await db.Analyses.FirstOrDefaultAsync(a => a.Id == _analysisId);
        var currentRowVersion = Convert.ToBase64String(analysis!.RowVersion);

        // Start the analysis first
        var startRequest = new StatusChangeRequest { Action = "Start", RowVersion = currentRowVersion };
        var startResponse = await _analystClient.PatchAsJsonAsync($"/api/v1/analyses/{_analysisId}/status", startRequest);
        Assert.Equal(System.Net.HttpStatusCode.OK, startResponse.StatusCode);

        var startContent = await startResponse.Content.ReadFromJsonAsync<AnalysisStatusDto>();
        Assert.NotNull(startContent);
        Assert.False(startContent.IsLocked);

        // Refresh to get updated row version
        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<LimsDbContext>();
        var analysisAfterStart = await db2.Analyses.FirstOrDefaultAsync(a => a.Id == _analysisId);
        var rowVersionAfterStart = Convert.ToBase64String(analysisAfterStart!.RowVersion);

        // Complete the analysis
        var completeRequest = new StatusChangeRequest { Action = "Complete", RowVersion = rowVersionAfterStart };
        var completeResponse = await _analystClient.PatchAsJsonAsync($"/api/v1/analyses/{_analysisId}/status", completeRequest);

        Assert.Equal(System.Net.HttpStatusCode.OK, completeResponse.StatusCode);

        var completeContent = await completeResponse.Content.ReadFromJsonAsync<AnalysisStatusDto>();
        Assert.NotNull(completeContent);
        Assert.Equal("Completed", completeContent.Status);
        Assert.True(completeContent.IsLocked);

        // Verify it's persisted as locked
        using var scope3 = _factory.Services.CreateScope();
        var db3 = scope3.ServiceProvider.GetRequiredService<LimsDbContext>();
        var persistedAnalysis = await db3.Analyses.FirstOrDefaultAsync(a => a.Id == _analysisId);
        Assert.True(persistedAnalysis!.IsLocked);
        Assert.NotNull(persistedAnalysis.LockedAtUtc);
        Assert.NotNull(persistedAnalysis.LockedByUserId);

        // Now unlock it as a Lab Coordinator
        var unlockRowVersion = Convert.ToBase64String(persistedAnalysis.RowVersion);

        var unlockRequest = new
        {
            justification = "Testing unlock flow",
            rowVersion = unlockRowVersion,
        };
        var unlockResponse = await _coordinatorClient.PatchAsJsonAsync(
            $"/api/v1/results/{_analysisId}/unlock",
            unlockRequest);

        Assert.Equal(System.Net.HttpStatusCode.OK, unlockResponse.StatusCode);

        var unlockContent = await unlockResponse.Content.ReadFromJsonAsync<UnlockResultDto>();
        Assert.NotNull(unlockContent);
        Assert.False(unlockContent.IsLocked);

        // Verify it's unlocked
        using var scope4 = _factory.Services.CreateScope();
        var db4 = scope4.ServiceProvider.GetRequiredService<LimsDbContext>();
        var unlockedAnalysis = await db4.Analyses.FirstOrDefaultAsync(a => a.Id == _analysisId);
        Assert.False(unlockedAnalysis!.IsLocked);
    }

    [Fact]
    public async Task ConcurrencyTestTwoWritesSameRowVersion_SecondReturns409()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        var analysis = await db.Analyses.FirstOrDefaultAsync(a => a.Id == _analysisId);
        var originalRowVersion = Convert.ToBase64String(analysis!.RowVersion);

        var request1 = new StatusChangeRequest { Action = "Start", RowVersion = originalRowVersion };
        var response1 = await _analystClient.PatchAsJsonAsync($"/api/v1/analyses/{_analysisId}/status", request1);
        Assert.Equal(System.Net.HttpStatusCode.OK, response1.StatusCode);

        var responseContent = await response1.Content.ReadFromJsonAsync<AnalysisStatusDto>();
        Assert.NotNull(responseContent);
        Assert.NotEqual(originalRowVersion, responseContent.RowVersion);

        var request2 = new StatusChangeRequest { Action = "Pause", RowVersion = originalRowVersion };
        var response2 = await _analystClient.PatchAsJsonAsync($"/api/v1/analyses/{_analysisId}/status", request2);

        Assert.Equal(System.Net.HttpStatusCode.Conflict, response2.StatusCode);
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
