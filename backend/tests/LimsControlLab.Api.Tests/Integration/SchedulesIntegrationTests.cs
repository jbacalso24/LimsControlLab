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

public sealed class SchedulesIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _coordinatorClient = null!;
    private HttpClient _analystClient = null!;
    private const string TestDbName = "cane-db-test-schedules";
    private int _scheduleId;
    private int _coordinatorId;
    private int _analystId;

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

        _coordinatorId = coordinator.Id;
        _analystId = analyst.Id;

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
    public async Task CreateScheduleByCoordinator_Returns201()
    {
        var request = new CreateScheduleRequest
        {
            Name = "TestSchedule",
            Site = "Inkerman",
            AnalysisType = "TestType",
            ShiftPattern = "Day",
            AssignedToUserId = _analystId,
        };

        var response = await _coordinatorClient.PostAsJsonAsync("/api/v1/schedules", request);

        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        var schedule = await db.Schedules.FirstOrDefaultAsync();
        Assert.NotNull(schedule);
        _scheduleId = schedule.Id;
    }

    [Fact]
    public async Task CreateScheduleByAnalyst_Returns403()
    {
        var request = new CreateScheduleRequest
        {
            Name = "TestSchedule",
            Site = "Inkerman",
            AnalysisType = "TestType",
            ShiftPattern = "Day",
            AssignedToUserId = _analystId,
        };

        var response = await _analystClient.PostAsJsonAsync("/api/v1/schedules", request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetScheduleById_Returns200()
    {
        await CreateTestSchedule();

        var response = await _analystClient.GetAsync($"/api/v1/schedules/{_scheduleId}");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListSchedules_Returns200()
    {
        await CreateTestSchedule();

        var response = await _analystClient.GetAsync("/api/v1/schedules");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateScheduleByCoordinator_Returns200()
    {
        await CreateTestSchedule();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        var schedule = await db.Schedules.FirstOrDefaultAsync();
        Assert.NotNull(schedule);
        var currentRowVersion = Convert.ToBase64String(schedule.RowVersion);

        var request = new UpdateScheduleRequest
        {
            Name = "UpdatedSchedule",
            AnalysisType = "UpdatedType",
            ShiftPattern = "Day",
            IsActive = true,
            RowVersion = currentRowVersion,
        };

        var response = await _coordinatorClient.PutAsJsonAsync($"/api/v1/schedules/{_scheduleId}", request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateScheduleByAnalyst_Returns403()
    {
        await CreateTestSchedule();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        var schedule = await db.Schedules.FirstOrDefaultAsync();
        Assert.NotNull(schedule);
        var currentRowVersion = Convert.ToBase64String(schedule.RowVersion);

        var request = new UpdateScheduleRequest
        {
            Name = "UpdatedSchedule",
            AnalysisType = "UpdatedType",
            ShiftPattern = "Day",
            IsActive = true,
            RowVersion = currentRowVersion,
        };

        var response = await _analystClient.PutAsJsonAsync($"/api/v1/schedules/{_scheduleId}", request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteScheduleByCoordinator_Returns204()
    {
        await CreateTestSchedule();

        var response = await _coordinatorClient.DeleteAsync($"/api/v1/schedules/{_scheduleId}");

        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AssignScheduleByCoordinator_Returns200()
    {
        await CreateTestSchedule();

        var request = new AssignScheduleRequest { UserId = _analystId };

        var response = await _coordinatorClient.PostAsJsonAsync($"/api/v1/schedules/{_scheduleId}/assign", request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ConcurrencyTestUpdateWithStaleRowVersion_Returns409()
    {
        await CreateTestSchedule();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        var schedule = await db.Schedules.FirstOrDefaultAsync();
        var originalRowVersion = Convert.ToBase64String(schedule!.RowVersion);

        var request1 = new UpdateScheduleRequest
        {
            Name = "FirstUpdate",
            AnalysisType = "Type1",
            ShiftPattern = "Day",
            IsActive = true,
            RowVersion = originalRowVersion,
        };

        var response1 = await _coordinatorClient.PutAsJsonAsync($"/api/v1/schedules/{_scheduleId}", request1);
        Assert.Equal(System.Net.HttpStatusCode.OK, response1.StatusCode);

        var request2 = new UpdateScheduleRequest
        {
            Name = "SecondUpdate",
            AnalysisType = "Type2",
            ShiftPattern = "Day",
            IsActive = true,
            RowVersion = originalRowVersion,
        };

        var response2 = await _coordinatorClient.PutAsJsonAsync($"/api/v1/schedules/{_scheduleId}", request2);

        Assert.Equal(System.Net.HttpStatusCode.Conflict, response2.StatusCode);
    }

    private async Task CreateTestSchedule()
    {
        var request = new CreateScheduleRequest
        {
            Name = "TestSchedule",
            Site = "Inkerman",
            AnalysisType = "TestType",
            ShiftPattern = "Day",
            AssignedToUserId = _analystId,
        };

        var response = await _coordinatorClient.PostAsJsonAsync("/api/v1/schedules", request);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        var schedule = await db.Schedules.FirstOrDefaultAsync();
        Assert.NotNull(schedule);
        _scheduleId = schedule.Id;
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
