using LimsControlLab.Domain.Auditing;
using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.Domain.Services;
using LimsControlLab.SharedKernel.Enums;
using Moq;
using Xunit;

namespace LimsControlLab.Api.Tests.Services;

#pragma warning disable CA1707
public sealed class SampleTransferServiceTests
#pragma warning restore CA1707
{
    [Fact]
    public async Task TransferAsyncWithValidRequest()
    {
        var mockRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();
        var timeProvider = TimeProvider.System;

        var sample = new Sample
        {
            Id = 1,
            Identifier = "S001",
            AnalysisTemplateId = 1,
            Status = LifecycleStatus.NotStarted,
            Site = Site.Inkerman,
            CurrentSite = Site.Inkerman,
            RowVersion = Array.Empty<byte>(),
        };

        mockRepository.Setup(r => r.GetSampleByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sample);
        mockRepository.Setup(r => r.TryAddSampleTransferAsync(It.IsAny<SampleTransfer>(), It.IsAny<Sample>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockCurrentUser.Setup(u => u.UserId).Returns(1);
        mockCurrentUser.Setup(u => u.Site).Returns(Site.Inkerman);
        mockCurrentUser.Setup(u => u.Role).Returns(Role.ControlLabAnalyst);

        var service = new SampleTransferService(mockRepository.Object, mockAuditLogger.Object, mockCurrentUser.Object, timeProvider);

        var result = await service.TransferAsync(1, Site.Proserpine, Array.Empty<byte>(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<Outcome<SampleTransferResult>.Ok>(result);
        var okResult = (Outcome<SampleTransferResult>.Ok)result;
        Assert.Equal(Site.Proserpine, okResult.Data.ToSite);
        Assert.Equal(Site.Inkerman, okResult.Data.FromSite);

        mockRepository.Verify(r => r.TryAddSampleTransferAsync(It.Is<SampleTransfer>(t =>
            t.SampleId == 1 && t.FromSite == Site.Inkerman && t.ToSite == Site.Proserpine), It.IsAny<Sample>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);

        mockAuditLogger.Verify(a => a.LogAsync(It.Is<AuditLogEntryRecord>(e =>
            e.Action == "SampleTransferred" && e.EntityType == "Sample" && e.EntityId == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransferAsyncWithWrongSiteReturnsForbidden()
    {
        var mockRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();
        var timeProvider = TimeProvider.System;

        var sample = new Sample
        {
            Id = 1,
            Identifier = "S001",
            AnalysisTemplateId = 1,
            Status = LifecycleStatus.NotStarted,
            Site = Site.Inkerman,
            CurrentSite = Site.Inkerman,
            RowVersion = Array.Empty<byte>(),
        };

        mockRepository.Setup(r => r.GetSampleByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sample);
        mockCurrentUser.Setup(u => u.Site).Returns(Site.Proserpine);
        mockCurrentUser.Setup(u => u.Role).Returns(Role.ControlLabAnalyst);

        var service = new SampleTransferService(mockRepository.Object, mockAuditLogger.Object, mockCurrentUser.Object, timeProvider);

        var result = await service.TransferAsync(1, Site.Proserpine, Array.Empty<byte>(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<Outcome<SampleTransferResult>.Forbidden>(result);
        mockAuditLogger.Verify(a => a.LogAsync(It.IsAny<AuditLogEntryRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransferAsyncWithSameSiteReturnsInvalid()
    {
        var mockRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();
        var timeProvider = TimeProvider.System;

        var sample = new Sample
        {
            Id = 1,
            Identifier = "S001",
            AnalysisTemplateId = 1,
            Status = LifecycleStatus.NotStarted,
            Site = Site.Inkerman,
            CurrentSite = Site.Inkerman,
            RowVersion = Array.Empty<byte>(),
        };

        mockRepository.Setup(r => r.GetSampleByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sample);
        mockCurrentUser.Setup(u => u.Site).Returns(Site.Inkerman);
        mockCurrentUser.Setup(u => u.Role).Returns(Role.ControlLabAnalyst);

        var service = new SampleTransferService(mockRepository.Object, mockAuditLogger.Object, mockCurrentUser.Object, timeProvider);

        var result = await service.TransferAsync(1, Site.Inkerman, Array.Empty<byte>(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<Outcome<SampleTransferResult>.Invalid>(result);
    }

    [Fact]
    public async Task TransferAsyncWithNonexistentSampleReturnsNotFound()
    {
        var mockRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();
        var timeProvider = TimeProvider.System;

        mockRepository.Setup(r => r.GetSampleByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sample?)null);

        var service = new SampleTransferService(mockRepository.Object, mockAuditLogger.Object, mockCurrentUser.Object, timeProvider);

        var result = await service.TransferAsync(999, Site.Proserpine, Array.Empty<byte>(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<Outcome<SampleTransferResult>.NotFound>(result);
    }
}
