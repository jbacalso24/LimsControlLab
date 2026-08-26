#pragma warning disable CA1707

using Moq;
using LimsControlLab.Domain.Auditing;
using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Calculations;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.Domain.Services;
using LimsControlLab.SharedKernel.Enums;
using Xunit;

namespace LimsControlLab.Api.Tests.Services;

public sealed class AnalysisExecutionServiceTests
{
    private static TimeProvider CreateTimeProvider() => TimeProvider.System;

    [Fact]
    public async Task CaptureReadingAsync_WithInToleranceValue_ReturnsOk()
    {
        var mockRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        var templateVersion = new AnalysisTemplateVersion
        {
            Id = 1,
            TemplateId = 1,
            Version = 1,
            MinTolerance = 1m,
            MaxTolerance = 5m,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        var analysis = new Analysis
        {
            Id = 1,
            SampleId = 1,
            TemplateId = 1,
            TemplateVersionId = 1,
            Status = LifecycleStatus.InProgress,
            StartedAtUtc = DateTimeOffset.UtcNow,
            StartedByUserId = 1,
            IsLocked = false,
            TemplateVersion = templateVersion,
        };

        mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(analysis);
        mockRepository.Setup(r => r.AddReadingAsync(It.IsAny<Reading>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockCurrentUser.Setup(u => u.UserId).Returns(1);
        mockCurrentUser.Setup(u => u.Role).Returns(Role.ControlLabAnalyst);

        var mockCalibrationRepository = new Mock<ICalibrationCurveRepository>();
        var service = new AnalysisExecutionService(
            mockRepository.Object,
            mockCalibrationRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var request = new CaptureReadingRequest
        {
            TestId = 1,
            Value = 3m,
            Unit = "mg",
            CapturedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await service.CaptureReadingAsync(1, request, CancellationToken.None);

        Assert.IsType<Outcome<ReadingCaptureResult>.Ok>(result);
        var ok = result as Outcome<ReadingCaptureResult>.Ok;
        Assert.True(ok!.Data.ValidationResult.IsValid);
        Assert.Equal("1-5", ok.Data.ValidationResult.ExpectedRange);
        Assert.Equal("3", ok.Data.ValidationResult.ActualValue);
        Assert.Null(ok.Data.ValidationResult.Reason);
        mockRepository.Verify(r => r.AddExceptionAsync(It.IsAny<ExceptionRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CaptureReadingAsync_OutOfTolerance_CreatesException()
    {
        var mockRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        var templateVersion = new AnalysisTemplateVersion
        {
            Id = 1,
            TemplateId = 1,
            Version = 1,
            MinTolerance = 1m,
            MaxTolerance = 5m,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        var analysis = new Analysis
        {
            Id = 1,
            SampleId = 1,
            TemplateId = 1,
            TemplateVersionId = 1,
            Status = LifecycleStatus.InProgress,
            StartedAtUtc = DateTimeOffset.UtcNow,
            StartedByUserId = 1,
            IsLocked = false,
            TemplateVersion = templateVersion,
        };

        mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(analysis);
        mockRepository.Setup(r => r.AddReadingAsync(It.IsAny<Reading>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockRepository.Setup(r => r.AddExceptionAsync(It.IsAny<ExceptionRecord>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockCurrentUser.Setup(u => u.UserId).Returns(1);
        mockCurrentUser.Setup(u => u.Role).Returns(Role.ControlLabAnalyst);

        var mockCalibrationRepository = new Mock<ICalibrationCurveRepository>();
        var service = new AnalysisExecutionService(
            mockRepository.Object,
            mockCalibrationRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var request = new CaptureReadingRequest
        {
            TestId = 1,
            Value = 10m,
            Unit = "mg",
            CapturedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await service.CaptureReadingAsync(1, request, CancellationToken.None);

        Assert.IsType<Outcome<ReadingCaptureResult>.Ok>(result);
        var ok = result as Outcome<ReadingCaptureResult>.Ok;
        Assert.False(ok!.Data.ValidationResult.IsValid);
        Assert.Equal("1-5", ok.Data.ValidationResult.ExpectedRange);
        Assert.Equal("10", ok.Data.ValidationResult.ActualValue);
        Assert.NotNull(ok.Data.ValidationResult.Reason);
        Assert.Contains("above maximum tolerance", ok.Data.ValidationResult.Reason);
        mockRepository.Verify(r => r.AddExceptionAsync(It.IsAny<ExceptionRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DecideExceptionAsync_WithoutComment_ReturnsInvalid()
    {
        var mockRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockCurrentUser.Setup(u => u.Role).Returns(Role.LabCoordinator);

        var mockCalibrationRepository = new Mock<ICalibrationCurveRepository>();
        var service = new AnalysisExecutionService(
            mockRepository.Object,
            mockCalibrationRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var request = new ExceptionDecisionRequest
        {
            Decision = "Modify",
            Comment = "",
            RowVersion = Convert.ToBase64String(new byte[8]),
        };

        var result = await service.DecideExceptionAsync(1, 1, request, CancellationToken.None);

        Assert.IsType<Outcome<ExceptionDecisionResult>.Invalid>(result);
    }

    [Fact]
    public async Task DecideExceptionAsync_NonCoordinator_ReturnsForbidden()
    {
        var mockRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockCurrentUser.Setup(u => u.Role).Returns(Role.ControlLabAnalyst);

        var mockCalibrationRepository = new Mock<ICalibrationCurveRepository>();
        var service = new AnalysisExecutionService(
            mockRepository.Object,
            mockCalibrationRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var request = new ExceptionDecisionRequest
        {
            Decision = "Modify",
            Comment = "Test",
            RowVersion = Convert.ToBase64String(new byte[8]),
        };

        var result = await service.DecideExceptionAsync(1, 1, request, CancellationToken.None);

        Assert.IsType<Outcome<ExceptionDecisionResult>.Forbidden>(result);
    }

    [Fact]
    public async Task CaptureReadingAsync_UnlockedAnalysis_WithCalibrationCurve_InterpolatesCalibratedValue()
    {
        // R39, R40: when an unlocked analysis captures a reading within a calibration curve's range,
        // the reading's CalibratedValue is computed via linear interpolation.
        var mockRepository = new Mock<IAnalysisRepository>();
        var mockCalibrationRepository = new Mock<ICalibrationCurveRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        var templateVersion = new AnalysisTemplateVersion
        {
            Id = 1,
            TemplateId = 1,
            Version = 1,
            MinTolerance = 1m,
            MaxTolerance = 20m,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        var analysis = new Analysis
        {
            Id = 1,
            SampleId = 1,
            TemplateId = 1,
            TemplateVersionId = 1,
            Status = LifecycleStatus.InProgress,
            StartedAtUtc = DateTimeOffset.UtcNow,
            StartedByUserId = 1,
            IsLocked = false,
            TemplateVersion = templateVersion,
        };

        var calibrationCurve = new CalibrationCurve
        {
            Id = 1,
            Name = "TestCurve",
            AnalysisTemplateId = 1,
            IsActive = true,
            Points = new[]
            {
                new CalibrationPoint { Id = 1, XValue = 0m, YValue = 0m, Order = 0 },
                new CalibrationPoint { Id = 2, XValue = 10m, YValue = 100m, Order = 1 },
            }.ToList(),
        };

        mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(analysis);
        mockRepository.Setup(r => r.AddReadingAsync(It.IsAny<Reading>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockCalibrationRepository.Setup(r => r.GetByAnalysisTemplateIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(calibrationCurve);
        mockCurrentUser.Setup(u => u.UserId).Returns(1);
        mockCurrentUser.Setup(u => u.Role).Returns(Role.ControlLabAnalyst);

        var service = new AnalysisExecutionService(
            mockRepository.Object,
            mockCalibrationRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var request = new CaptureReadingRequest
        {
            TestId = 1,
            Value = 5m, // Halfway between 0 and 10
            Unit = "mg",
            CapturedAtUtc = DateTimeOffset.UtcNow,
        };

        Reading capturedReading = null!;
        mockRepository.Setup(r => r.AddReadingAsync(It.IsAny<Reading>(), It.IsAny<CancellationToken>()))
            .Callback<Reading, CancellationToken>((r, _) => capturedReading = r)
            .Returns(Task.CompletedTask);

        var result = await service.CaptureReadingAsync(1, request, CancellationToken.None);

        Assert.IsType<Outcome<ReadingCaptureResult>.Ok>(result);
        // Calibrated value should be interpolated: y = 0 + (5 - 0) * (100 - 0) / (10 - 0) = 50
        Assert.NotNull(capturedReading);
        Assert.Equal(50m, capturedReading.CalibratedValue);
    }

    [Fact]
    public async Task CaptureReadingAsync_LockedAnalysis_DoesNotComputeCalibratedValue()
    {
        // R57, charter §2: when an analysis is locked, derived values freeze and do not recompute.
        var mockRepository = new Mock<IAnalysisRepository>();
        var mockCalibrationRepository = new Mock<ICalibrationCurveRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        var templateVersion = new AnalysisTemplateVersion
        {
            Id = 1,
            TemplateId = 1,
            Version = 1,
            MinTolerance = 1m,
            MaxTolerance = 20m,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        var analysis = new Analysis
        {
            Id = 1,
            SampleId = 1,
            TemplateId = 1,
            TemplateVersionId = 1,
            Status = LifecycleStatus.Completed,
            StartedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            StartedByUserId = 1,
            IsLocked = true,
            LockedAtUtc = DateTimeOffset.UtcNow,
            LockedByUserId = 2,
            TemplateVersion = templateVersion,
        };

        var calibrationCurve = new CalibrationCurve
        {
            Id = 1,
            Name = "TestCurve",
            AnalysisTemplateId = 1,
            IsActive = true,
            Points = new[]
            {
                new CalibrationPoint { Id = 1, XValue = 0m, YValue = 0m, Order = 0 },
                new CalibrationPoint { Id = 2, XValue = 10m, YValue = 100m, Order = 1 },
            }.ToList(),
        };

        mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(analysis);
        mockRepository.Setup(r => r.AddReadingAsync(It.IsAny<Reading>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        // CalibrationRepository should NOT be called when locked
        mockCalibrationRepository.Setup(r => r.GetByAnalysisTemplateIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(calibrationCurve);
        mockCurrentUser.Setup(u => u.UserId).Returns(1);
        mockCurrentUser.Setup(u => u.Role).Returns(Role.ControlLabAnalyst);

        var service = new AnalysisExecutionService(
            mockRepository.Object,
            mockCalibrationRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var request = new CaptureReadingRequest
        {
            TestId = 1,
            Value = 5m,
            Unit = "mg",
            CapturedAtUtc = DateTimeOffset.UtcNow,
        };

        Reading capturedReading = null!;
        mockRepository.Setup(r => r.AddReadingAsync(It.IsAny<Reading>(), It.IsAny<CancellationToken>()))
            .Callback<Reading, CancellationToken>((r, _) => capturedReading = r)
            .Returns(Task.CompletedTask);

        var result = await service.CaptureReadingAsync(1, request, CancellationToken.None);

        Assert.IsType<Outcome<ReadingCaptureResult>.Ok>(result);
        // Reading is captured but CalibratedValue stays null (recomputation did not run)
        Assert.NotNull(capturedReading);
        Assert.Null(capturedReading.CalibratedValue);
        // Calibration repository should not be queried when locked
        mockCalibrationRepository.Verify(
            r => r.GetByAnalysisTemplateIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeStatusAsync_WithCompleteAction_SetsIsLockedTrue()
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
            Status = LifecycleStatus.InProgress,
            StartedAtUtc = DateTimeOffset.UtcNow,
            StartedByUserId = 1,
            IsLocked = false,
            RowVersion = new byte[8],
        };

        mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(analysis);
        mockRepository.Setup(r => r.TryUpdateAnalysisWithConcurrencyCheckAsync(It.IsAny<Analysis>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mockCurrentUser.Setup(u => u.UserId).Returns(2);
        mockCurrentUser.Setup(u => u.Role).Returns(Role.ControlLabAnalyst);

        var mockCalibrationRepository = new Mock<ICalibrationCurveRepository>();
        var service = new AnalysisExecutionService(
            mockRepository.Object,
            mockCalibrationRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var request = new StatusChangeRequest
        {
            Action = "Complete",
            RowVersion = Convert.ToBase64String(analysis.RowVersion),
        };

        var result = await service.ChangeStatusAsync(1, request, CancellationToken.None);

        Assert.IsType<Outcome<AnalysisStatusChangeResult>.Ok>(result);
        var ok = result as Outcome<AnalysisStatusChangeResult>.Ok;
        Assert.NotNull(ok);
        Assert.Equal("Completed", ok.Data.Status);
        Assert.True(ok.Data.IsLocked);

        mockRepository.Verify(
            r => r.TryUpdateAnalysisWithConcurrencyCheckAsync(
                It.Is<Analysis>(a => a.IsLocked && a.Status == LifecycleStatus.Completed && a.LockedByUserId == 2),
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAnalysisDetailAsync_RecomputesValidationDetailUsingBoundTemplateVersion()
    {
        var mockRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        var templateVersion = new AnalysisTemplateVersion
        {
            Id = 1,
            TemplateId = 1,
            Version = 1,
            MinTolerance = 98m,
            MaxTolerance = 99.5m,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        var analysis = new Analysis
        {
            Id = 1,
            SampleId = 1,
            TemplateId = 1,
            TemplateVersionId = 1,
            Status = LifecycleStatus.InProgress,
            StartedAtUtc = DateTimeOffset.UtcNow,
            StartedByUserId = 1,
            IsLocked = false,
            TemplateVersion = templateVersion,
            Readings = new List<Reading>
            {
                new Reading
                {
                    Id = 1,
                    AnalysisId = 1,
                    TestId = 1,
                    Value = 50m,
                    Unit = "mg",
                    CapturedAtUtc = DateTimeOffset.UtcNow,
                    CapturedByUserId = 1,
                    ValidationResult = "OutOfTolerance",
                },
                new Reading
                {
                    Id = 2,
                    AnalysisId = 1,
                    TestId = 2,
                    Value = 98.5m,
                    Unit = "mg",
                    CapturedAtUtc = DateTimeOffset.UtcNow,
                    CapturedByUserId = 1,
                    ValidationResult = "Valid",
                },
            },
            Exceptions = new List<ExceptionRecord>(),
        };

        mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(analysis);
        mockCurrentUser.Setup(u => u.UserId).Returns(1);
        mockCurrentUser.Setup(u => u.Role).Returns(Role.ControlLabAnalyst);

        var mockCalibrationRepository = new Mock<ICalibrationCurveRepository>();
        var service = new AnalysisExecutionService(
            mockRepository.Object,
            mockCalibrationRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var result = await service.GetAnalysisDetailAsync(1, CancellationToken.None);

        Assert.IsType<Outcome<AnalysisDetailResult>.Ok>(result);
        var ok = result as Outcome<AnalysisDetailResult>.Ok;
        Assert.NotNull(ok);
        Assert.Equal(2, ok.Data.Readings.Count);

        var invalidReading = ok.Data.Readings.First();
        Assert.False(invalidReading.ValidationResult.IsValid);
        Assert.Equal("98-99.5", invalidReading.ValidationResult.ExpectedRange);
        Assert.Equal("50", invalidReading.ValidationResult.ActualValue);
        Assert.NotNull(invalidReading.ValidationResult.Reason);
        Assert.Contains("below minimum tolerance", invalidReading.ValidationResult.Reason);

        var validReading = ok.Data.Readings.Last();
        Assert.True(validReading.ValidationResult.IsValid);
        Assert.Equal("98-99.5", validReading.ValidationResult.ExpectedRange);
        Assert.Equal("98.5", validReading.ValidationResult.ActualValue);
        Assert.Null(validReading.ValidationResult.Reason);
    }
}
