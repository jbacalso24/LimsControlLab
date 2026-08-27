#pragma warning disable CA1707

using Moq;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Integration;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.Domain.Services;
using LimsControlLab.SharedKernel.Enums;
using Xunit;

namespace LimsControlLab.Api.Tests.Services;

public sealed class IntegrationMonitoringServiceTests
{
    private static IntegrationLogEntry Log(int id, string target, string status, int analysisId = 1) =>
        new()
        {
            Id = id,
            TargetSystem = target,
            AnalysisId = analysisId,
            Status = status,
            AttemptedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = 0,
        };

    // Build real transmission services whose dependencies are mocked. When a test does not
    // exercise reprocessing, these are never invoked.
    private static DatabankIntegrationService BuildDatabank(bool sinkResult = false, Analysis? analysis = null, AnalysisTemplate? template = null)
    {
        var sink = new Mock<IDatabankSink>();
        sink.Setup(s => s.TransmitAnalysisAsync(It.IsAny<Analysis>(), It.IsAny<AnalysisTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sinkResult);
        var repo = new Mock<IAnalysisRepository>();
        if (analysis != null)
            repo.Setup(r => r.GetByIdAsync(analysis.Id, It.IsAny<CancellationToken>())).ReturnsAsync(analysis);
        if (template != null)
            repo.Setup(r => r.GetTemplateByIdAsync(template.Id, It.IsAny<CancellationToken>())).ReturnsAsync(template);
        var logRepo = new Mock<IIntegrationLogRepository>();
        return new DatabankIntegrationService(sink.Object, repo.Object, logRepo.Object, TimeProvider.System);
    }

    private static ScadaPushService BuildScada()
    {
        var sink = new Mock<ISCADASink>();
        var repo = new Mock<IAnalysisRepository>();
        var logRepo = new Mock<IIntegrationLogRepository>();
        return new ScadaPushService(sink.Object, repo.Object, logRepo.Object, TimeProvider.System);
    }

    [Fact]
    public async Task ListAsync_MapsEntriesAndPassesFilters()
    {
        var mockRepo = new Mock<IIntegrationLogRepository>();
        mockRepo
            .Setup(r => r.ListAsync("Failed", "Databank", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Log(1, "Databank", "Failed"), Log(2, "Databank", "Failed") })
            .Verifiable();

        var service = new IntegrationMonitoringService(mockRepo.Object, BuildDatabank(), BuildScada());

        var result = await service.ListAsync("Failed", "Databank", CancellationToken.None);

        var ok = Assert.IsType<Outcome<List<IntegrationLogItem>>.Ok>(result);
        Assert.Equal(2, ok.Data.Count);
        Assert.All(ok.Data, i => Assert.Equal("Databank", i.TargetSystem));
        mockRepo.Verify();
    }

    [Fact]
    public async Task ReprocessAsync_ReturnsNotFound_WhenLogMissing()
    {
        var mockRepo = new Mock<IIntegrationLogRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IntegrationLogEntry?)null);

        var service = new IntegrationMonitoringService(mockRepo.Object, BuildDatabank(), BuildScada());

        var result = await service.ReprocessAsync(42, CancellationToken.None);

        Assert.IsType<Outcome<ReprocessResult>.NotFound>(result);
    }

    [Fact]
    public async Task ReprocessAsync_ReturnsInvalid_ForUnsupportedTarget()
    {
        var mockRepo = new Mock<IIntegrationLogRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Log(5, "DataLakehouse", "Failed"));

        var service = new IntegrationMonitoringService(mockRepo.Object, BuildDatabank(), BuildScada());

        var result = await service.ReprocessAsync(5, CancellationToken.None);

        var invalid = Assert.IsType<Outcome<ReprocessResult>.Invalid>(result);
        Assert.Equal("targetSystem", invalid.Field);
    }

    [Fact]
    public async Task ReprocessAsync_RoutesToDatabank_AndReportsSuccess()
    {
        var analysis = new Analysis
        {
            Id = 1,
            SampleId = 1,
            TemplateId = 9,
            TemplateVersionId = 1,
            Status = LifecycleStatus.Completed,
            StartedAtUtc = DateTimeOffset.UtcNow,
            StartedByUserId = 1,
            IsLocked = true,
        };
        var template = new AnalysisTemplate { Id = 9, Name = "Sugar Pol", Site = Site.Inkerman, IsRetired = false };

        var mockRepo = new Mock<IIntegrationLogRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Log(3, "Databank", "Failed", analysisId: 1));

        var service = new IntegrationMonitoringService(
            mockRepo.Object,
            BuildDatabank(sinkResult: true, analysis: analysis, template: template),
            BuildScada());

        var result = await service.ReprocessAsync(3, CancellationToken.None);

        var ok = Assert.IsType<Outcome<ReprocessResult>.Ok>(result);
        Assert.True(ok.Data.Success);
        Assert.Equal("Success", ok.Data.Status);
    }
}
