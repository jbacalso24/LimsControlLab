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

    private static IUserRepository CreateUserRepository() => new Mock<IUserRepository>().Object;

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
            CreateTimeProvider(),
            CreateUserRepository());

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
            CreateTimeProvider(),
            CreateUserRepository());

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
            CreateTimeProvider(),
            CreateUserRepository());

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
            CreateTimeProvider(),
            CreateUserRepository());

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
            CreateTimeProvider(),
            CreateUserRepository());

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
            CreateTimeProvider(),
            CreateUserRepository());

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
            CreateTimeProvider(),
            CreateUserRepository());

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
            CreateTimeProvider(),
            CreateUserRepository());

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

    [Fact]
    public async Task GetAnalysisDetailAsync_ResolvesCapturedByUsernameFromUserRepository()
    {
        var mockRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();
        var mockCalibrationRepository = new Mock<ICalibrationCurveRepository>();
        var mockUserRepository = new Mock<IUserRepository>();

        var templateVersion = new AnalysisTemplateVersion
        {
            Id = 1,
            TemplateId = 1,
            Version = 1,
            MinTolerance = 1m,
            MaxTolerance = 100m,
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
            StartedByUserId = 7,
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
                    CapturedByUserId = 7,
                    ValidationResult = "Valid",
                },
                new Reading
                {
                    Id = 2,
                    AnalysisId = 1,
                    TestId = 2,
                    Value = 60m,
                    Unit = "mg",
                    CapturedAtUtc = DateTimeOffset.UtcNow,
                    CapturedByUserId = 999,
                    ValidationResult = "Valid",
                },
            },
            Exceptions = new List<ExceptionRecord>(),
        };

        mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(analysis);
        mockUserRepository
            .Setup(u => u.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 7,
                Username = "invicta_analyst",
                PasswordHash = "hash",
                Role = Role.ControlLabAnalyst,
                Site = Site.Invicta,
            });
        // User 999 is intentionally unknown to verify the id fallback.
        mockUserRepository
            .Setup(u => u.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = new AnalysisExecutionService(
            mockRepository.Object,
            mockCalibrationRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider(),
            mockUserRepository.Object);

        var result = await service.GetAnalysisDetailAsync(1, CancellationToken.None);

        var ok = Assert.IsType<Outcome<AnalysisDetailResult>.Ok>(result);
        Assert.Equal("invicta_analyst", ok.Data.Readings.First(r => r.Id == 1).CapturedByUsername);
        // Falls back to the raw id string when the user cannot be resolved.
        Assert.Equal("999", ok.Data.Readings.First(r => r.Id == 2).CapturedByUsername);
    }

    private static Analysis BuildAnalysisWithTestConfig(string? testConfiguration)
    {
        var templateVersion = new AnalysisTemplateVersion
        {
            Id = 1,
            TemplateId = 1,
            Version = 1,
            MinTolerance = 0m,
            MaxTolerance = 1000m,
            TestConfiguration = testConfiguration,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        return new Analysis
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
            Readings = new List<Reading>(),
            Exceptions = new List<ExceptionRecord>(),
        };
    }

    private static AnalysisExecutionService BuildService(
        Mock<IAnalysisRepository> mockRepository,
        Mock<ICurrentUser>? mockCurrentUser = null)
    {
        mockCurrentUser ??= new Mock<ICurrentUser>();
        return new AnalysisExecutionService(
            mockRepository.Object,
            new Mock<ICalibrationCurveRepository>().Object,
            new Mock<IAuditLogger>().Object,
            mockCurrentUser.Object,
            CreateTimeProvider(),
            new Mock<IUserRepository>().Object);
    }

    [Fact]
    public async Task GetAnalysisDetailAsync_ParsesAvailableTestsFromTemplateConfiguration()
    {
        var config = "{\"tests\":[{\"id\":1,\"name\":\"Pol\",\"unit\":\"°Z\",\"method\":\"BSES\"},{\"id\":2,\"name\":\"Temperature\",\"unit\":\"°C\"}],\"sampleMethod\":\"Single (snap)\"}";
        var analysis = BuildAnalysisWithTestConfig(config);
        var mockRepository = new Mock<IAnalysisRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(analysis);
        var service = BuildService(mockRepository);

        var result = await service.GetAnalysisDetailAsync(1, CancellationToken.None);

        var ok = Assert.IsType<Outcome<AnalysisDetailResult>.Ok>(result);
        Assert.Equal(2, ok.Data.AvailableTests.Count);
        var pol = ok.Data.AvailableTests[0];
        Assert.Equal(1, pol.Id);
        Assert.Equal("Pol", pol.Name);
        Assert.Equal("°Z", pol.Unit);
        Assert.Equal("BSES", pol.Method);
        Assert.Null(ok.Data.AvailableTests[1].Method);
    }

    [Fact]
    public async Task GetAnalysisDetailAsync_ReturnsEmptyAvailableTests_WhenConfigMalformed()
    {
        var analysis = BuildAnalysisWithTestConfig("{ not valid json");
        var mockRepository = new Mock<IAnalysisRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(analysis);
        var service = BuildService(mockRepository);

        var result = await service.GetAnalysisDetailAsync(1, CancellationToken.None);

        var ok = Assert.IsType<Outcome<AnalysisDetailResult>.Ok>(result);
        Assert.Empty(ok.Data.AvailableTests);
    }

    [Fact]
    public async Task CaptureReadingAsync_RejectsTestIdNotDefinedByTemplate()
    {
        var config = "{\"tests\":[{\"id\":1,\"name\":\"Pol\",\"unit\":\"°Z\"}]}";
        var analysis = BuildAnalysisWithTestConfig(config);
        var mockRepository = new Mock<IAnalysisRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(analysis);
        var mockCurrentUser = new Mock<ICurrentUser>();
        mockCurrentUser.Setup(u => u.UserId).Returns(1);
        mockCurrentUser.Setup(u => u.Role).Returns(Role.ControlLabAnalyst);
        var service = BuildService(mockRepository, mockCurrentUser);

        var request = new CaptureReadingRequest
        {
            TestId = 99,
            Value = 50m,
            Unit = "fake",
            CapturedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await service.CaptureReadingAsync(1, request, CancellationToken.None);

        var invalid = Assert.IsType<Outcome<ReadingCaptureResult>.Invalid>(result);
        Assert.Equal("testId", invalid.Field);
    }

    [Fact]
    public async Task CaptureReadingAsync_SetsUnitFromTestDefinition_IgnoringClientUnit()
    {
        var config = "{\"tests\":[{\"id\":1,\"name\":\"Pol\",\"unit\":\"°Z\"}]}";
        var analysis = BuildAnalysisWithTestConfig(config);
        Reading? captured = null;
        var mockRepository = new Mock<IAnalysisRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(analysis);
        mockRepository
            .Setup(r => r.AddReadingAsync(It.IsAny<Reading>(), It.IsAny<CancellationToken>()))
            .Callback<Reading, CancellationToken>((r, _) => captured = r)
            .Returns(Task.CompletedTask);
        var mockCurrentUser = new Mock<ICurrentUser>();
        mockCurrentUser.Setup(u => u.UserId).Returns(1);
        mockCurrentUser.Setup(u => u.Role).Returns(Role.ControlLabAnalyst);
        var service = BuildService(mockRepository, mockCurrentUser);

        var request = new CaptureReadingRequest
        {
            TestId = 1,
            Value = 50m,
            Unit = "wrong-unit",
            CapturedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await service.CaptureReadingAsync(1, request, CancellationToken.None);

        Assert.IsType<Outcome<ReadingCaptureResult>.Ok>(result);
        Assert.NotNull(captured);
        Assert.Equal("°Z", captured!.Unit);
    }

    private static AnalysisTemplate BuildTemplate(Site site = Site.Inkerman, bool retired = false, int? currentVersionId = 5) =>
        new() { Id = 1, Name = "Sugar Pol", Site = site, IsRetired = retired, CurrentVersionId = currentVersionId };

    private static (Mock<IAnalysisRepository> repo, Mock<ICurrentUser> user) AdHocMocks(AnalysisTemplate template, Site userSite = Site.Inkerman)
    {
        var repo = new Mock<IAnalysisRepository>();
        repo.Setup(r => r.GetTemplateByIdAsync(template.Id, It.IsAny<CancellationToken>())).ReturnsAsync(template);
        repo.Setup(r => r.SampleIdentifierExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(r => r.CountSamplesBySiteAsync(It.IsAny<Site>(), It.IsAny<CancellationToken>())).ReturnsAsync(6);
        repo.Setup(r => r.AddSampleAsync(It.IsAny<Sample>(), It.IsAny<CancellationToken>()))
            .Callback<Sample, CancellationToken>((s, _) => s.Id = 500).Returns(Task.CompletedTask);
        repo.Setup(r => r.AddAnalysisAsync(It.IsAny<Analysis>(), It.IsAny<CancellationToken>()))
            .Callback<Analysis, CancellationToken>((a, _) => a.Id = 900).Returns(Task.CompletedTask);
        var user = new Mock<ICurrentUser>();
        user.Setup(u => u.UserId).Returns(7);
        user.Setup(u => u.Role).Returns(Role.ControlLabAnalyst);
        user.Setup(u => u.Site).Returns(userSite);
        return (repo, user);
    }

    [Fact]
    public async Task CreateAdHocAnalysisAsync_CreatesSampleAndAnalysisBoundToCurrentVersion()
    {
        var template = BuildTemplate(currentVersionId: 5);
        var (repo, user) = AdHocMocks(template);
        Sample? createdSample = null;
        Analysis? createdAnalysis = null;
        repo.Setup(r => r.AddSampleAsync(It.IsAny<Sample>(), It.IsAny<CancellationToken>()))
            .Callback<Sample, CancellationToken>((s, _) => { s.Id = 500; createdSample = s; }).Returns(Task.CompletedTask);
        repo.Setup(r => r.AddAnalysisAsync(It.IsAny<Analysis>(), It.IsAny<CancellationToken>()))
            .Callback<Analysis, CancellationToken>((a, _) => { a.Id = 900; createdAnalysis = a; }).Returns(Task.CompletedTask);
        var service = BuildService(repo, user);

        var result = await service.CreateAdHocAnalysisAsync(1, null, CancellationToken.None);

        var ok = Assert.IsType<Outcome<AdHocAnalysisResult>.Ok>(result);
        Assert.Equal(900, ok.Data.AnalysisId);
        Assert.Equal(500, ok.Data.SampleId);
        Assert.StartsWith("INK-", ok.Data.SampleIdentifier); // auto-generated for Inkerman
        Assert.NotNull(createdAnalysis);
        Assert.Equal(5, createdAnalysis!.TemplateVersionId);
        Assert.Equal(LifecycleStatus.InProgress, createdAnalysis.Status);
        Assert.NotNull(createdSample);
        Assert.Equal(Site.Inkerman, createdSample!.Site);
    }

    [Fact]
    public async Task CreateAdHocAnalysisAsync_RejectsTemplateFromAnotherSite()
    {
        var template = BuildTemplate(site: Site.Invicta);
        var (repo, user) = AdHocMocks(template, userSite: Site.Inkerman);
        var service = BuildService(repo, user);

        var result = await service.CreateAdHocAnalysisAsync(1, null, CancellationToken.None);

        var invalid = Assert.IsType<Outcome<AdHocAnalysisResult>.Invalid>(result);
        Assert.Equal("analysisTemplateId", invalid.Field);
    }

    [Fact]
    public async Task CreateAdHocAnalysisAsync_RejectsTemplateWithNoActiveVersion()
    {
        var template = BuildTemplate(currentVersionId: null);
        var (repo, user) = AdHocMocks(template);
        var service = BuildService(repo, user);

        var result = await service.CreateAdHocAnalysisAsync(1, null, CancellationToken.None);

        Assert.IsType<Outcome<AdHocAnalysisResult>.Invalid>(result);
    }

    [Fact]
    public async Task CreateAdHocAnalysisAsync_RejectsDuplicateProvidedIdentifier()
    {
        var template = BuildTemplate();
        var (repo, user) = AdHocMocks(template);
        repo.Setup(r => r.SampleIdentifierExistsAsync("INK-DUP", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = BuildService(repo, user);

        var result = await service.CreateAdHocAnalysisAsync(1, "INK-DUP", CancellationToken.None);

        var invalid = Assert.IsType<Outcome<AdHocAnalysisResult>.Invalid>(result);
        Assert.Equal("sampleIdentifier", invalid.Field);
    }
}
