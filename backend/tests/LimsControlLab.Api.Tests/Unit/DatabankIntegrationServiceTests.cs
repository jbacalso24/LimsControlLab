#pragma warning disable CA1707

using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Integration;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.Domain.Services;
using LimsControlLab.SharedKernel.Enums;
using Moq;
using Xunit;

namespace LimsControlLab.Api.Tests.Unit;

public sealed class DatabankIntegrationServiceTests
{
    private readonly Mock<IDatabankSink> _mockSink = new();
    private readonly Mock<IAnalysisRepository> _mockAnalysisRepository = new();
    private readonly Mock<IIntegrationLogRepository> _mockLogRepository = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public async Task TransmitAnalysisAsync_ReturnsTrue_WhenAnalysisIsLockedAndComplete()
    {
        // Arrange: a locked, completed analysis
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
        };

        var template = new AnalysisTemplate
        {
            Id = 1,
            Name = "Sugar Analysis",
            Site = Site.Inkerman,
            IsRetired = false,
        };

        _mockAnalysisRepository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);
        _mockAnalysisRepository.Setup(x => x.GetTemplateByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _mockSink.Setup(x => x.TransmitAnalysisAsync(analysis, template, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new DatabankIntegrationService(
            _mockSink.Object,
            _mockAnalysisRepository.Object,
            _mockLogRepository.Object,
            _timeProvider);

        // Act
        var result = await service.TransmitAnalysisAsync(1, CancellationToken.None);

        // Assert: transmission succeeded and log entry was created with Success status
        Assert.True(result);
        _mockSink.Verify(x => x.TransmitAnalysisAsync(analysis, template, It.IsAny<CancellationToken>()), Times.Once);
        _mockLogRepository.Verify(x => x.AddAsync(It.IsAny<IntegrationLogEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockLogRepository.Verify(x => x.UpdateAsync(
            It.Is<IntegrationLogEntry>(e => e.Status == "Success"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransmitAnalysisAsync_ReturnsFalse_WhenAnalysisIsNotLocked()
    {
        // Arrange: an unlocked analysis (R52 — only locked analyses transmit)
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
            IsLocked = false,  // Not locked — should reject
            LockedAtUtc = null,
            LockedByUserId = null,
        };

        var template = new AnalysisTemplate
        {
            Id = 1,
            Name = "Sugar Analysis",
            Site = Site.Inkerman,
            IsRetired = false,
        };

        _mockAnalysisRepository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);
        _mockAnalysisRepository.Setup(x => x.GetTemplateByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var service = new DatabankIntegrationService(
            _mockSink.Object,
            _mockAnalysisRepository.Object,
            _mockLogRepository.Object,
            _timeProvider);

        // Act
        var result = await service.TransmitAnalysisAsync(1, CancellationToken.None);

        // Assert: transmission rejected, sink was never called
        Assert.False(result);
        _mockSink.Verify(x => x.TransmitAnalysisAsync(It.IsAny<Analysis>(), It.IsAny<AnalysisTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransmitAnalysisAsync_ReturnsFalse_WhenAnalysisStatusIsNotCompleted()
    {
        // Arrange: a locked but in-progress analysis
        var analysis = new Analysis
        {
            Id = 1,
            SampleId = 1,
            TemplateId = 1,
            TemplateVersionId = 1,
            Status = LifecycleStatus.InProgress,  // Not completed
            StartedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            CompletedAtUtc = null,
            StartedByUserId = 1,
            IsLocked = true,
            LockedAtUtc = null,
            LockedByUserId = null,
        };

        var template = new AnalysisTemplate
        {
            Id = 1,
            Name = "Sugar Analysis",
            Site = Site.Inkerman,
            IsRetired = false,
        };

        _mockAnalysisRepository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);
        _mockAnalysisRepository.Setup(x => x.GetTemplateByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var service = new DatabankIntegrationService(
            _mockSink.Object,
            _mockAnalysisRepository.Object,
            _mockLogRepository.Object,
            _timeProvider);

        // Act
        var result = await service.TransmitAnalysisAsync(1, CancellationToken.None);

        // Assert: transmission rejected
        Assert.False(result);
        _mockSink.Verify(x => x.TransmitAnalysisAsync(It.IsAny<Analysis>(), It.IsAny<AnalysisTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransmitAnalysisAsync_ReturnsFalse_WhenTemplatIsCMolassesExchange()
    {
        // Arrange: a locked, completed analysis with C Molasses Exchange template (R57 — excluded this release)
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
        };

        var template = new AnalysisTemplate
        {
            Id = 1,
            Name = "C Molasses Exchange",  // Excluded per R57
            Site = Site.Inkerman,
            IsRetired = false,
        };

        _mockAnalysisRepository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);
        _mockAnalysisRepository.Setup(x => x.GetTemplateByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var service = new DatabankIntegrationService(
            _mockSink.Object,
            _mockAnalysisRepository.Object,
            _mockLogRepository.Object,
            _timeProvider);

        // Act
        var result = await service.TransmitAnalysisAsync(1, CancellationToken.None);

        // Assert: transmission excluded for C Molasses Exchange, sink not called
        Assert.False(result);
        _mockSink.Verify(x => x.TransmitAnalysisAsync(It.IsAny<Analysis>(), It.IsAny<AnalysisTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransmitAnalysisAsync_MixedBatch_OnlyNonExcludedAnalysesTransmit()
    {
        // Arrange: two locked, completed analyses — one C Molasses Exchange (excluded), one regular (included)
        var sugarAnalysis = new Analysis
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
        };

        var sugarTemplate = new AnalysisTemplate
        {
            Id = 1,
            Name = "Sugar Analysis",
            Site = Site.Inkerman,
            IsRetired = false,
        };

        var molassesAnalysis = new Analysis
        {
            Id = 2,
            SampleId = 2,
            TemplateId = 2,
            TemplateVersionId = 2,
            Status = LifecycleStatus.Completed,
            StartedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            CompletedAtUtc = DateTimeOffset.UtcNow,
            StartedByUserId = 1,
            IsLocked = true,
            LockedAtUtc = DateTimeOffset.UtcNow,
            LockedByUserId = 1,
        };

        var molassesTemplate = new AnalysisTemplate
        {
            Id = 2,
            Name = "C Molasses Exchange",  // Excluded
            Site = Site.Inkerman,
            IsRetired = false,
        };

        var service = new DatabankIntegrationService(
            _mockSink.Object,
            _mockAnalysisRepository.Object,
            _mockLogRepository.Object,
            _timeProvider);

        // Act: transmit sugar analysis
        _mockAnalysisRepository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sugarAnalysis);
        _mockAnalysisRepository.Setup(x => x.GetTemplateByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sugarTemplate);
        _mockSink.Setup(x => x.TransmitAnalysisAsync(sugarAnalysis, sugarTemplate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sugarResult = await service.TransmitAnalysisAsync(1, CancellationToken.None);

        // Act: attempt molasses analysis
        _mockAnalysisRepository.Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(molassesAnalysis);
        _mockAnalysisRepository.Setup(x => x.GetTemplateByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(molassesTemplate);

        var molassesResult = await service.TransmitAnalysisAsync(2, CancellationToken.None);

        // Assert: sugar transmitted, molasses excluded
        Assert.True(sugarResult);
        Assert.False(molassesResult);
        _mockSink.Verify(x => x.TransmitAnalysisAsync(sugarAnalysis, sugarTemplate, It.IsAny<CancellationToken>()), Times.Once);
        _mockSink.Verify(x => x.TransmitAnalysisAsync(molassesAnalysis, molassesTemplate, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransmitAnalysisAsync_LogsFailure_WhenSinkThrows()
    {
        // Arrange: a locked, completed analysis; sink throws
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
        };

        var template = new AnalysisTemplate
        {
            Id = 1,
            Name = "Sugar Analysis",
            Site = Site.Inkerman,
            IsRetired = false,
        };

        _mockAnalysisRepository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);
        _mockAnalysisRepository.Setup(x => x.GetTemplateByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _mockSink.Setup(x => x.TransmitAnalysisAsync(analysis, template, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Transmission failed"));

        var service = new DatabankIntegrationService(
            _mockSink.Object,
            _mockAnalysisRepository.Object,
            _mockLogRepository.Object,
            _timeProvider);

        // Act
        var result = await service.TransmitAnalysisAsync(1, CancellationToken.None);

        // Assert: failure logged with error message (R53)
        Assert.False(result);
        _mockLogRepository.Verify(x => x.UpdateAsync(
            It.Is<IntegrationLogEntry>(e => e.Status == "Failed" && e.ErrorMessage == "Transmission failed"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
