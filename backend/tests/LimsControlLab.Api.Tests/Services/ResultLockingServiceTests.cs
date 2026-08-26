#pragma warning disable CA1707

using Moq;
using LimsControlLab.Domain.Auditing;
using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.Domain.Services;
using LimsControlLab.SharedKernel.Enums;
using Xunit;

namespace LimsControlLab.Api.Tests.Services;

public sealed class ResultLockingServiceTests
{
    private static TimeProvider CreateTimeProvider() => TimeProvider.System;

    [Fact]
    public async Task UnlockResultAsync_WithValidRequest_UnlocksAnalysis()
    {
        var mockRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        var analysis = new Analysis
        {
            Id = 1,
            SampleId = 1,
            TemplateId = 1,
            TemplateVersionId = 1,
            Status = LifecycleStatus.Completed,
            StartedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            CompletedAtUtc = DateTimeOffset.UtcNow,
            StartedByUserId = 1,
            IsLocked = true,
            LockedAtUtc = DateTimeOffset.UtcNow,
            LockedByUserId = 1,
            RowVersion = new byte[] { 1 },
        };

        mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(analysis);
        mockRepository.Setup(r => r.TryUpdateAnalysisWithConcurrencyCheckAsync(It.IsAny<Analysis>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockCurrentUser.Setup(u => u.UserId).Returns(2);
        mockCurrentUser.Setup(u => u.Role).Returns(Role.LabCoordinator);

        var service = new ResultLockingService(
            mockRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var request = new UnlockResultRequest
        {
            Justification = "Test unlock justification",
            RowVersion = Convert.ToBase64String(analysis.RowVersion),
        };

        var result = await service.UnlockResultAsync(1, request, CancellationToken.None);

        Assert.IsType<Outcome<ResultUnlockResult>.Ok>(result);
        if (result is Outcome<ResultUnlockResult>.Ok ok)
        {
            Assert.False(ok.Data.IsLocked);
        }
        mockAuditLogger.Verify(
            a => a.LogAsync(It.IsAny<AuditLogEntryRecord>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UnlockResultAsync_WithoutJustification_ReturnsInvalid()
    {
        var mockRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockCurrentUser.Setup(u => u.Role).Returns(Role.LabCoordinator);

        var service = new ResultLockingService(
            mockRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var request = new UnlockResultRequest
        {
            Justification = string.Empty,
            RowVersion = Convert.ToBase64String(new byte[] { 1 }),
        };

        var result = await service.UnlockResultAsync(1, request, CancellationToken.None);

        Assert.IsType<Outcome<ResultUnlockResult>.Invalid>(result);
    }

    [Fact]
    public async Task UnlockResultAsync_WithAnalystRole_ReturnsForbidden()
    {
        var mockRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockCurrentUser.Setup(u => u.Role).Returns(Role.ControlLabAnalyst);

        var service = new ResultLockingService(
            mockRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var request = new UnlockResultRequest
        {
            Justification = "Test justification",
            RowVersion = Convert.ToBase64String(new byte[] { 1 }),
        };

        var result = await service.UnlockResultAsync(1, request, CancellationToken.None);

        Assert.IsType<Outcome<ResultUnlockResult>.Forbidden>(result);
    }

    [Fact]
    public async Task UnlockResultAsync_WithNonexistentAnalysis_ReturnsNotFound()
    {
        var mockRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Analysis?)null);
        mockCurrentUser.Setup(u => u.Role).Returns(Role.LabCoordinator);

        var service = new ResultLockingService(
            mockRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var request = new UnlockResultRequest
        {
            Justification = "Test justification",
            RowVersion = Convert.ToBase64String(new byte[] { 1 }),
        };

        var result = await service.UnlockResultAsync(1, request, CancellationToken.None);

        Assert.IsType<Outcome<ResultUnlockResult>.NotFound>(result);
    }

    [Fact]
    public async Task UnlockResultAsync_WithUnlockedAnalysis_ReturnsInvalid()
    {
        var mockRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        var analysis = new Analysis
        {
            Id = 1,
            SampleId = 1,
            TemplateId = 1,
            TemplateVersionId = 1,
            Status = LifecycleStatus.Completed,
            StartedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            CompletedAtUtc = DateTimeOffset.UtcNow,
            StartedByUserId = 1,
            IsLocked = false,
            RowVersion = new byte[] { 1 },
        };

        mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(analysis);
        mockCurrentUser.Setup(u => u.Role).Returns(Role.LabCoordinator);

        var service = new ResultLockingService(
            mockRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var request = new UnlockResultRequest
        {
            Justification = "Test justification",
            RowVersion = Convert.ToBase64String(analysis.RowVersion),
        };

        var result = await service.UnlockResultAsync(1, request, CancellationToken.None);

        Assert.IsType<Outcome<ResultUnlockResult>.Invalid>(result);
    }

    [Fact]
    public async Task UnlockResultAsync_WithConcurrencyConflict_ReturnsConflict()
    {
        var mockRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        var lockedAnalysis = new Analysis
        {
            Id = 1,
            SampleId = 1,
            TemplateId = 1,
            TemplateVersionId = 1,
            Status = LifecycleStatus.Completed,
            StartedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            CompletedAtUtc = DateTimeOffset.UtcNow,
            StartedByUserId = 1,
            IsLocked = true,
            LockedAtUtc = DateTimeOffset.UtcNow,
            LockedByUserId = 1,
            RowVersion = new byte[] { 1 },
        };

        var currentAnalysis = new Analysis
        {
            Id = 1,
            SampleId = 1,
            TemplateId = 1,
            TemplateVersionId = 1,
            Status = LifecycleStatus.Completed,
            StartedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            CompletedAtUtc = DateTimeOffset.UtcNow,
            StartedByUserId = 1,
            IsLocked = true,
            LockedAtUtc = DateTimeOffset.UtcNow,
            LockedByUserId = 1,
            RowVersion = new byte[] { 2 },
        };

        mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockedAnalysis);
        mockRepository.Setup(r => r.TryUpdateAnalysisWithConcurrencyCheckAsync(It.IsAny<Analysis>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentAnalysis);
        mockCurrentUser.Setup(u => u.UserId).Returns(2);
        mockCurrentUser.Setup(u => u.Role).Returns(Role.LabCoordinator);

        var service = new ResultLockingService(
            mockRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var request = new UnlockResultRequest
        {
            Justification = "Test justification",
            RowVersion = Convert.ToBase64String(lockedAnalysis.RowVersion),
        };

        var result = await service.UnlockResultAsync(1, request, CancellationToken.None);

        Assert.IsType<Outcome<ResultUnlockResult>.Conflict>(result);
    }
}
