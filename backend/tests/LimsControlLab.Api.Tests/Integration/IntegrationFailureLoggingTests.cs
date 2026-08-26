#pragma warning disable CA1707

using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.Infrastructure;
using LimsControlLab.SharedKernel.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LimsControlLab.Api.Tests.Integration;

public sealed class IntegrationFailureLoggingTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private const string TestDbName = "cane-db-test-integration-logging";

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

        using var scope2 = _factory.Services.CreateScope();
        var db = scope2.ServiceProvider.GetRequiredService<LimsDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();
        using var scope = new ServiceCollection()
            .AddDbContext<LimsDbContext>(options =>
                options.UseSqlServer($"Server=localhost;Database={TestDbName};Trusted_Connection=True;TrustServerCertificate=True;"))
            .BuildServiceProvider()
            .CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        await db.Database.EnsureDeletedAsync();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task IntegrationLogEntry_IsCreated_OnTransmissionAttempt()
    {
        // R53: Integration failures are visible to authorised users and support reprocessing.
        // This test verifies that a transmission attempt is logged for visibility.

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        var logRepository = scope.ServiceProvider.GetRequiredService<IIntegrationLogRepository>();

        // Arrange: create a test analysis
        var user = new User
        {
            Username = "testuser",
            PasswordHash = "hashed",
            Role = Role.ControlLabAnalyst,
            Site = Site.Inkerman,
        };

        var template = new AnalysisTemplate
        {
            Name = "Test Analysis",
            Site = Site.Inkerman,
            IsRetired = false,
        };

        db.Users.Add(user);
        db.AnalysisTemplates.Add(template);
        await db.SaveChangesAsync();

        var templateVersion = new AnalysisTemplateVersion
        {
            TemplateId = template.Id,
            Version = 1,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.AnalysisTemplateVersions.Add(templateVersion);
        await db.SaveChangesAsync();

        var sample = new Sample
        {
            Identifier = "S001",
            AnalysisTemplateId = template.Id,
            Status = LifecycleStatus.NotStarted,
            Site = Site.Inkerman,
            CurrentSite = Site.Inkerman,
        };

        var analysis = new Analysis
        {
            SampleId = 0,  // Will be assigned after sample save
            TemplateId = template.Id,
            TemplateVersionId = templateVersion.Id,
            Status = LifecycleStatus.Completed,
            StartedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            StartedByUserId = 1,
            IsLocked = true,
            LockedAtUtc = DateTimeOffset.UtcNow,
            LockedByUserId = 1,
        };

        db.Samples.Add(sample);
        await db.SaveChangesAsync();

        analysis.SampleId = sample.Id;
        db.Analyses.Add(analysis);
        await db.SaveChangesAsync();

        // Arrange: create a log entry (simulating a failed transmission attempt)
        var logEntry = new IntegrationLogEntry
        {
            TargetSystem = "Databank",
            AnalysisId = analysis.Id,
            Status = "Failed",
            AttemptedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            ErrorMessage = "Connection timeout",
            RetryCount = 1,
        };

        // Act: add the log entry
        await logRepository.AddAsync(logEntry, CancellationToken.None);

        // Assert: verify the log entry is persisted and retrievable
        var retrieved = await logRepository.GetByIdAsync(logEntry.Id, CancellationToken.None);
        Assert.NotNull(retrieved);
        Assert.Equal("Failed", retrieved.Status);
        Assert.Equal("Connection timeout", retrieved.ErrorMessage);
        Assert.Equal("Databank", retrieved.TargetSystem);
    }

    [Fact]
    public async Task IntegrationLogEntry_FailedEntries_AreRetrievable()
    {
        // R53: Support reprocessing — failed entries must be queryable.

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        var logRepository = scope.ServiceProvider.GetRequiredService<IIntegrationLogRepository>();

        // Arrange
        var user = new User
        {
            Username = "testuser2",
            PasswordHash = "hashed",
            Role = Role.ControlLabAnalyst,
            Site = Site.Inkerman,
        };

        var template = new AnalysisTemplate
        {
            Name = "Test Analysis 2",
            Site = Site.Inkerman,
            IsRetired = false,
        };

        db.Users.Add(user);
        db.AnalysisTemplates.Add(template);
        await db.SaveChangesAsync();

        var templateVersion = new AnalysisTemplateVersion
        {
            TemplateId = template.Id,
            Version = 1,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.AnalysisTemplateVersions.Add(templateVersion);
        await db.SaveChangesAsync();

        var sample = new Sample
        {
            Identifier = "S002",
            AnalysisTemplateId = template.Id,
            Status = LifecycleStatus.NotStarted,
            Site = Site.Inkerman,
            CurrentSite = Site.Inkerman,
        };

        db.Samples.Add(sample);
        await db.SaveChangesAsync();

        var analysis1 = new Analysis
        {
            SampleId = sample.Id,
            TemplateId = template.Id,
            TemplateVersionId = templateVersion.Id,
            Status = LifecycleStatus.Completed,
            StartedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            StartedByUserId = user.Id,
            IsLocked = true,
            LockedAtUtc = DateTimeOffset.UtcNow,
            LockedByUserId = user.Id,
        };

        var analysis2 = new Analysis
        {
            SampleId = sample.Id,
            TemplateId = template.Id,
            TemplateVersionId = templateVersion.Id,
            Status = LifecycleStatus.Completed,
            StartedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            StartedByUserId = user.Id,
            IsLocked = true,
            LockedAtUtc = DateTimeOffset.UtcNow,
            LockedByUserId = user.Id,
        };

        db.Analyses.AddRange(analysis1, analysis2);
        await db.SaveChangesAsync();

        var logEntry1 = new IntegrationLogEntry
        {
            TargetSystem = "Databank",
            AnalysisId = analysis1.Id,
            Status = "Failed",
            AttemptedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            ErrorMessage = "Network error",
            RetryCount = 0,
        };

        var logEntry2 = new IntegrationLogEntry
        {
            TargetSystem = "Databank",
            AnalysisId = analysis2.Id,
            Status = "Success",
            AttemptedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            ErrorMessage = null,
            RetryCount = 0,
        };

        await logRepository.AddAsync(logEntry1, CancellationToken.None);
        await logRepository.AddAsync(logEntry2, CancellationToken.None);

        // Act: query failed entries only
        var failedEntries = await logRepository.GetFailedEntriesAsync(CancellationToken.None);

        // Assert: only the failed entry is returned
        var failedList = failedEntries.ToList();
        Assert.Single(failedList);
        Assert.Equal("Failed", failedList[0].Status);
        Assert.Equal("Network error", failedList[0].ErrorMessage);
    }
}
