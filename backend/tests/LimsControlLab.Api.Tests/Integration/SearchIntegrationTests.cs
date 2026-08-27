#pragma warning disable CA1707

using System.IdentityModel.Tokens.Jwt;
using System.Text;
using LimsControlLab.Api;
using LimsControlLab.Api.Auth;
using LimsControlLab.Api.Common;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Infrastructure;
using LimsControlLab.SharedKernel.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace LimsControlLab.Api.Tests.Integration;

public sealed class SearchIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _analystClient = null!;
    private const string TestDbName = "cane-db-test-search";

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

        db.Users.Add(analyst);
        await db.SaveChangesAsync();

        var token = CreateToken(analyst.Id, analyst.Username, analyst.Role, analyst.Site);
        _analystClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var template = new AnalysisTemplate
        {
            Name = "Sugar Analysis",
            Site = Site.Inkerman,
            IsRetired = false,
        };

        db.AnalysisTemplates.Add(template);
        await db.SaveChangesAsync();

        var version = new AnalysisTemplateVersion
        {
            TemplateId = template.Id,
            Version = 1,
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
            StartedAtUtc = DateTimeOffset.UtcNow.AddDays(-5),
            StartedByUserId = analyst.Id,
            IsLocked = false,
        };

        db.Analyses.Add(analysis);
        await db.SaveChangesAsync();

        var instrument = new Instrument
        {
            Name = "Polarimeter",
            Site = Site.Inkerman,
            IsActive = true,
        };

        db.Instruments.Add(instrument);
        await db.SaveChangesAsync();

        var reading = new Reading
        {
            AnalysisId = analysis.Id,
            TestId = 1,
            Value = 16.5m,
            Unit = "% pol",
            CapturedAtUtc = analysis.StartedAtUtc.AddHours(1),
            CapturedByUserId = analyst.Id,
            InstrumentId = instrument.Id,
            ValidationResult = "Pass",
        };

        db.Readings.Add(reading);
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _factory.Dispose();
        _analystClient.Dispose();
    }

    [Fact]
    public async Task Search_UnauthorizedRequest_Returns401()
    {
        var client = _factory.CreateClient();
        var request = new { };

        var response = await client.PostAsJsonAsync("/api/v1/search/results", request);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithValidRequest_Returns200()
    {
        var request = new { };

        var response = await _analystClient.PostAsJsonAsync("/api/v1/search/results", request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<PagedResult<object>>();
        Assert.NotNull(content);
        Assert.True(content.TotalCount >= 1);
    }

    [Fact]
    public async Task Search_WithPageSize_ClampsToMax()
    {
        var request = new { };
        var response = await _analystClient.PostAsJsonAsync("/api/v1/search/results?pageNumber=1&pageSize=10000", request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<PagedResult<object>>();
        Assert.NotNull(content);
        Assert.True(content.PageSize <= 500);
    }

    [Fact]
    public async Task Search_WithTemplateName_FiltersResults()
    {
        var request = new { templateName = "Sugar" };

        var response = await _analystClient.PostAsJsonAsync("/api/v1/search/results", request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<PagedResult<object>>();
        Assert.NotNull(content);
        Assert.True(content.TotalCount >= 1);
    }

    [Fact]
    public async Task Search_WithTemplateNameNotMatching_ReturnsEmpty()
    {
        var request = new { templateName = "NonExistent" };

        var response = await _analystClient.PostAsJsonAsync("/api/v1/search/results", request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<PagedResult<object>>();
        Assert.NotNull(content);
        Assert.Equal(0, content.TotalCount);
    }

    [Fact]
    public async Task Search_WithTestId_FiltersResults()
    {
        var request = new { testId = 1 };

        var response = await _analystClient.PostAsJsonAsync("/api/v1/search/results", request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<PagedResult<object>>();
        Assert.NotNull(content);
        Assert.True(content.TotalCount >= 1);
    }

    [Fact]
    public async Task Search_WithWrongTestId_ReturnsEmpty()
    {
        var request = new { testId = 999 };

        var response = await _analystClient.PostAsJsonAsync("/api/v1/search/results", request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<PagedResult<object>>();
        Assert.NotNull(content);
        Assert.Equal(0, content.TotalCount);
    }

    [Fact]
    public async Task Search_WithSampleIdentifier_FiltersResults()
    {
        var request = new { sampleIdentifier = "S001" };

        var response = await _analystClient.PostAsJsonAsync("/api/v1/search/results", request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<PagedResult<object>>();
        Assert.NotNull(content);
        Assert.True(content.TotalCount >= 1);
    }

    [Fact]
    public async Task Search_WithDateRange_FiltersResults()
    {
        var fromDate = DateTimeOffset.UtcNow.AddDays(-10);
        var toDate = DateTimeOffset.UtcNow;

        var request = new { fromUtc = fromDate, toUtc = toDate };

        var response = await _analystClient.PostAsJsonAsync("/api/v1/search/results", request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<PagedResult<object>>();
        Assert.NotNull(content);
        Assert.True(content.TotalCount >= 1);
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
