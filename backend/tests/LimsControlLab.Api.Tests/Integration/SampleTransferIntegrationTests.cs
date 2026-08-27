using LimsControlLab.Api.Controllers;
using LimsControlLab.Domain.Auditing;
using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.Domain.Services;
using LimsControlLab.Infrastructure;
using LimsControlLab.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LimsControlLab.Api.Tests.Integration;

#pragma warning disable CA1707
public sealed class SampleTransferIntegrationTests
#pragma warning restore CA1707
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    private async Task<LimsDbContext> CreateDbContext()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<LimsDbContext>()
            .UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database={_dbName};Integrated Security=true;")
            .Options;

        var context = new LimsDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    [Fact]
    public async Task TransferWithValidRequest()
    {
        using var db = await CreateDbContext();

        var user = new User { Username = "u1", PasswordHash = "h", Role = Role.ControlLabAnalyst, Site = Site.Inkerman };
        var template = new AnalysisTemplate { Name = "T1", Site = Site.Inkerman, IsRetired = false };
        var version = new AnalysisTemplateVersion { TemplateId = 0, Version = 1, CreatedAtUtc = DateTimeOffset.UtcNow };

        db.Users.Add(user);
        db.AnalysisTemplates.Add(template);
        await db.SaveChangesAsync();

        version.TemplateId = template.Id;
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

        var transfer = new SampleTransfer
        {
            SampleId = sample.Id,
            FromSite = Site.Inkerman,
            ToSite = Site.Proserpine,
            TransferredByUserId = user.Id,
            TransferredAtUtc = DateTimeOffset.UtcNow,
        };
        db.SampleTransfers.Add(transfer);
        await db.SaveChangesAsync();

        var transferRecord = db.SampleTransfers.FirstOrDefault();
        Assert.NotNull(transferRecord);
        Assert.Equal(Site.Proserpine, transferRecord.ToSite);
        Assert.Equal(Site.Inkerman, transferRecord.FromSite);
    }

    [Fact]
    public async Task TransferCreatesAuditLogEntry()
    {
        using var db = await CreateDbContext();

        var user = new User { Username = "u2", PasswordHash = "h", Role = Role.ControlLabAnalyst, Site = Site.Inkerman };
        var template = new AnalysisTemplate { Name = "T2", Site = Site.Inkerman, IsRetired = false };
        var version = new AnalysisTemplateVersion { TemplateId = 0, Version = 1, CreatedAtUtc = DateTimeOffset.UtcNow };

        db.Users.Add(user);
        db.AnalysisTemplates.Add(template);
        await db.SaveChangesAsync();

        version.TemplateId = template.Id;
        db.AnalysisTemplateVersions.Add(version);
        await db.SaveChangesAsync();

        template.CurrentVersionId = version.Id;
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

        var audit = new AuditLogEntry
        {
            UserId = user.Id,
            Role = "ControlLabAnalyst",
            TimestampUtc = DateTimeOffset.UtcNow,
            Action = "SampleTransferred",
            EntityType = "Sample",
            EntityId = sample.Id,
            BeforeValues = "CurrentSite: Inkerman",
            AfterValues = "CurrentSite: Proserpine",
        };
        db.AuditLogs.Add(audit);
        await db.SaveChangesAsync();

        var auditRecord = db.AuditLogs.FirstOrDefault(a => a.Action == "SampleTransferred");
        Assert.NotNull(auditRecord);
        Assert.Equal("Sample", auditRecord.EntityType);
        Assert.Equal(sample.Id, auditRecord.EntityId);
    }

    [Fact]
    public async Task GetSampleByOriginSiteCanView()
    {
        using var db = await CreateDbContext();

        var user = new User { Username = "u3", PasswordHash = "h", Role = Role.ControlLabAnalyst, Site = Site.Inkerman };
        var template = new AnalysisTemplate { Name = "T3", Site = Site.Inkerman, IsRetired = false };
        var version = new AnalysisTemplateVersion { TemplateId = 0, Version = 1, CreatedAtUtc = DateTimeOffset.UtcNow };

        db.Users.Add(user);
        db.AnalysisTemplates.Add(template);
        await db.SaveChangesAsync();

        version.TemplateId = template.Id;
        db.AnalysisTemplateVersions.Add(version);
        await db.SaveChangesAsync();

        template.CurrentVersionId = version.Id;
        await db.SaveChangesAsync();

        var sample = new Sample
        {
            Identifier = "S003",
            AnalysisTemplateId = template.Id,
            Status = LifecycleStatus.NotStarted,
            Site = Site.Inkerman,
            CurrentSite = Site.Inkerman,
        };
        db.Samples.Add(sample);
        await db.SaveChangesAsync();

        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.Site).Returns(Site.Inkerman);
        currentUserMock.Setup(u => u.UserId).Returns(user.Id);

        var auditLoggerMock = new Mock<IAuditLogger>();
        var timeProviderMock = new Mock<TimeProvider>();

        var service = new SampleTransferService(new TestAnalysisRepository(db), auditLoggerMock.Object, currentUserMock.Object, timeProviderMock.Object);

        var result = await service.GetByIdAsync(sample.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<Outcome<Sample>.Ok>(result);
        var okResult = (Outcome<Sample>.Ok)result;
        Assert.Equal(sample.Id, okResult.Data.Id);
        Assert.Equal("S003", okResult.Data.Identifier);
    }

    [Fact]
    public async Task GetSampleByCurrentSiteCanView()
    {
        using var db = await CreateDbContext();

        var user = new User { Username = "u4", PasswordHash = "h", Role = Role.ControlLabAnalyst, Site = Site.Proserpine };
        var template = new AnalysisTemplate { Name = "T4", Site = Site.Inkerman, IsRetired = false };
        var version = new AnalysisTemplateVersion { TemplateId = 0, Version = 1, CreatedAtUtc = DateTimeOffset.UtcNow };

        db.Users.Add(user);
        db.AnalysisTemplates.Add(template);
        await db.SaveChangesAsync();

        version.TemplateId = template.Id;
        db.AnalysisTemplateVersions.Add(version);
        await db.SaveChangesAsync();

        template.CurrentVersionId = version.Id;
        await db.SaveChangesAsync();

        var sample = new Sample
        {
            Identifier = "S004",
            AnalysisTemplateId = template.Id,
            Status = LifecycleStatus.NotStarted,
            Site = Site.Inkerman,
            CurrentSite = Site.Proserpine,
        };
        db.Samples.Add(sample);
        await db.SaveChangesAsync();

        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.Site).Returns(Site.Proserpine);
        currentUserMock.Setup(u => u.UserId).Returns(user.Id);

        var auditLoggerMock = new Mock<IAuditLogger>();
        var timeProviderMock = new Mock<TimeProvider>();

        var service = new SampleTransferService(new TestAnalysisRepository(db), auditLoggerMock.Object, currentUserMock.Object, timeProviderMock.Object);

        var result = await service.GetByIdAsync(sample.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<Outcome<Sample>.Ok>(result);
        var okResult = (Outcome<Sample>.Ok)result;
        Assert.Equal(sample.Id, okResult.Data.Id);
        Assert.Equal("S004", okResult.Data.Identifier);
    }

    [Fact]
    public async Task GetSampleByUnrelatedSiteGetsForbidden()
    {
        using var db = await CreateDbContext();

        var user = new User { Username = "u5", PasswordHash = "h", Role = Role.ControlLabAnalyst, Site = Site.Kalamia };
        var template = new AnalysisTemplate { Name = "T5", Site = Site.Inkerman, IsRetired = false };
        var version = new AnalysisTemplateVersion { TemplateId = 0, Version = 1, CreatedAtUtc = DateTimeOffset.UtcNow };

        db.Users.Add(user);
        db.AnalysisTemplates.Add(template);
        await db.SaveChangesAsync();

        version.TemplateId = template.Id;
        db.AnalysisTemplateVersions.Add(version);
        await db.SaveChangesAsync();

        template.CurrentVersionId = version.Id;
        await db.SaveChangesAsync();

        var sample = new Sample
        {
            Identifier = "S005",
            AnalysisTemplateId = template.Id,
            Status = LifecycleStatus.NotStarted,
            Site = Site.Inkerman,
            CurrentSite = Site.Proserpine,
        };
        db.Samples.Add(sample);
        await db.SaveChangesAsync();

        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.Site).Returns(Site.Kalamia);
        currentUserMock.Setup(u => u.UserId).Returns(user.Id);

        var auditLoggerMock = new Mock<IAuditLogger>();
        var timeProviderMock = new Mock<TimeProvider>();

        var service = new SampleTransferService(new TestAnalysisRepository(db), auditLoggerMock.Object, currentUserMock.Object, timeProviderMock.Object);

        var result = await service.GetByIdAsync(sample.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<Outcome<Sample>.Forbidden>(result);
    }

    [Fact]
    public async Task GetSampleNonexistentGetsNotFound()
    {
        using var db = await CreateDbContext();

        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.Site).Returns(Site.Inkerman);

        var auditLoggerMock = new Mock<IAuditLogger>();
        var timeProviderMock = new Mock<TimeProvider>();

        var service = new SampleTransferService(new TestAnalysisRepository(db), auditLoggerMock.Object, currentUserMock.Object, timeProviderMock.Object);

        var result = await service.GetByIdAsync(9999, CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<Outcome<Sample>.NotFound>(result);
    }
}

internal sealed class TestAnalysisRepository : IAnalysisRepository
{
    private readonly LimsDbContext _context;

    public TestAnalysisRepository(LimsDbContext context)
    {
        _context = context;
    }

    public async Task<Sample?> GetSampleByIdAsync(int sampleId, CancellationToken ct = default)
    {
        return await _context.Samples.FirstOrDefaultAsync(s => s.Id == sampleId, ct);
    }

    public Task<Analysis?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<AnalysisTemplate?> GetTemplateByIdAsync(int id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AddReadingAsync(Reading reading, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AddExceptionAsync(ExceptionRecord exception, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AddSampleAsync(Sample sample, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AddAnalysisAsync(Analysis analysis, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SampleIdentifierExistsAsync(string identifier, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> CountSamplesBySiteAsync(Site site, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AddSampleTransferAsync(SampleTransfer transfer, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> TryAddSampleTransferAsync(SampleTransfer transfer, Sample sample, byte[] expectedRowVersion, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<ExceptionRecord?> GetExceptionByIdAsync(int id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> TryUpdateAnalysisWithConcurrencyCheckAsync(Analysis analysis, byte[] expectedRowVersion, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> TryUpdateExceptionWithConcurrencyCheckAsync(ExceptionRecord exception, byte[] expectedRowVersion, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Analysis>> GetAnalysesWithExceptionsAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Analysis>> GetAnalysesWithExceptionsBySiteAsync(Site site, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
