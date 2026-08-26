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

public sealed class CalibrationCurveServiceTests
{
    private static TimeProvider CreateTimeProvider() => TimeProvider.System;

    [Fact]
    public async Task CreateAsync_LabCoordinator_WithValidPoints_ReturnsOk()
    {
        var mockRepository = new Mock<ICalibrationCurveRepository>();
        var mockAnalysisRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockCurrentUser.Setup(u => u.UserId).Returns(1);
        mockCurrentUser.Setup(u => u.Role).Returns(Role.LabCoordinator);

        var service = new CalibrationCurveService(
            mockRepository.Object,
            mockAnalysisRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var points = new List<(decimal, decimal)>
        {
            (0m, 0m),
            (10m, 100m),
        };

        var result = await service.CreateAsync("TestCurve", 1, points, CancellationToken.None);

        Assert.IsType<Outcome<CalibrationCurveDto>.Ok>(result);
        mockRepository.Verify(r => r.AddAsync(It.IsAny<CalibrationCurve>(), It.IsAny<CancellationToken>()), Times.Once);
        mockAuditLogger.Verify(
            a => a.LogAsync(
                It.Is<AuditLogEntryRecord>(x => x.Action == "CalibrationCurveCreated"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ControlLabAnalyst_ReturnsForbidden()
    {
        var mockRepository = new Mock<ICalibrationCurveRepository>();
        var mockAnalysisRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockCurrentUser.Setup(u => u.Role).Returns(Role.ControlLabAnalyst);

        var service = new CalibrationCurveService(
            mockRepository.Object,
            mockAnalysisRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var points = new List<(decimal, decimal)> { (0m, 0m) };

        var result = await service.CreateAsync("TestCurve", 1, points, CancellationToken.None);

        Assert.IsType<Outcome<CalibrationCurveDto>.Forbidden>(result);
    }

    [Fact]
    public async Task CreateAsync_EmptyName_ReturnsInvalid()
    {
        var mockRepository = new Mock<ICalibrationCurveRepository>();
        var mockAnalysisRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockCurrentUser.Setup(u => u.Role).Returns(Role.LabCoordinator);

        var service = new CalibrationCurveService(
            mockRepository.Object,
            mockAnalysisRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var points = new List<(decimal, decimal)> { (0m, 0m) };

        var result = await service.CreateAsync("", 1, points, CancellationToken.None);

        Assert.IsType<Outcome<CalibrationCurveDto>.Invalid>(result);
    }

    [Fact]
    public async Task CreateAsync_NoPoints_ReturnsInvalid()
    {
        var mockRepository = new Mock<ICalibrationCurveRepository>();
        var mockAnalysisRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockCurrentUser.Setup(u => u.Role).Returns(Role.LabCoordinator);

        var service = new CalibrationCurveService(
            mockRepository.Object,
            mockAnalysisRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var points = new List<(decimal, decimal)>();

        var result = await service.CreateAsync("TestCurve", 1, points, CancellationToken.None);

        Assert.IsType<Outcome<CalibrationCurveDto>.Invalid>(result);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingCurve_ReturnsOk()
    {
        var mockRepository = new Mock<ICalibrationCurveRepository>();
        var mockAnalysisRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        var curve = new CalibrationCurve
        {
            Id = 1,
            Name = "TestCurve",
            AnalysisTemplateId = 1,
            IsActive = true,
            RowVersion = System.Text.Encoding.UTF8.GetBytes("v1"),
            Points = new[]
            {
                new CalibrationPoint { Id = 1, XValue = 0m, YValue = 0m, Order = 0 },
                new CalibrationPoint { Id = 2, XValue = 10m, YValue = 100m, Order = 1 },
            }.ToList(),
        };

        mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(curve);

        var service = new CalibrationCurveService(
            mockRepository.Object,
            mockAnalysisRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var result = await service.GetByIdAsync(1, CancellationToken.None);

        Assert.IsType<Outcome<CalibrationCurveDto>.Ok>(result);
        var ok = (Outcome<CalibrationCurveDto>.Ok)result;
        Assert.Equal(1, ok.Data.Id);
        Assert.Equal("TestCurve", ok.Data.Name);
        Assert.Equal(2, ok.Data.PointCount);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingCurve_ReturnsNotFound()
    {
        var mockRepository = new Mock<ICalibrationCurveRepository>();
        var mockAnalysisRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((CalibrationCurve?)null);

        var service = new CalibrationCurveService(
            mockRepository.Object,
            mockAnalysisRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var result = await service.GetByIdAsync(1, CancellationToken.None);

        Assert.IsType<Outcome<CalibrationCurveDto>.NotFound>(result);
    }

    [Fact]
    public async Task DeactivateAsync_LabCoordinator_ReturnsOk()
    {
        var mockRepository = new Mock<ICalibrationCurveRepository>();
        var mockAnalysisRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockCurrentUser.Setup(u => u.UserId).Returns(1);
        mockCurrentUser.Setup(u => u.Role).Returns(Role.LabCoordinator);

        var curve = new CalibrationCurve
        {
            Id = 1,
            Name = "TestCurve",
            AnalysisTemplateId = 1,
            IsActive = true,
        };

        mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(curve);

        var service = new CalibrationCurveService(
            mockRepository.Object,
            mockAnalysisRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var result = await service.DeactivateAsync(1, CancellationToken.None);

        Assert.IsType<Outcome<bool>.Ok>(result);
        mockRepository.Verify(r => r.UpdateAsync(It.IsAny<CalibrationCurve>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_ReturnsCurvesWithPointsAndTemplateInfo()
    {
        var mockRepository = new Mock<ICalibrationCurveRepository>();
        var mockAnalysisRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        var templateA = new AnalysisTemplate { Id = 1, Name = "TemplateA", Site = Site.Inkerman, IsRetired = false };
        var templateB = new AnalysisTemplate { Id = 2, Name = "TemplateB", Site = Site.Proserpine, IsRetired = false };

        var curveA = new CalibrationCurve
        {
            Id = 1,
            Name = "CurveA",
            AnalysisTemplateId = 1,
            IsActive = true,
            RowVersion = System.Text.Encoding.UTF8.GetBytes("v1"),
            AnalysisTemplate = templateA,
            Points = new[]
            {
                new CalibrationPoint { Id = 1, XValue = 0m, YValue = 0m, Order = 0 },
                new CalibrationPoint { Id = 2, XValue = 10m, YValue = 100m, Order = 1 },
            }.ToList(),
        };

        var curveB = new CalibrationCurve
        {
            Id = 2,
            Name = "CurveB",
            AnalysisTemplateId = 2,
            IsActive = false,
            RowVersion = System.Text.Encoding.UTF8.GetBytes("v2"),
            AnalysisTemplate = templateB,
            Points = new[]
            {
                new CalibrationPoint { Id = 3, XValue = 1m, YValue = 2m, Order = 0 },
            }.ToList(),
        };

        mockRepository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CalibrationCurve> { curveA, curveB });

        var service = new CalibrationCurveService(
            mockRepository.Object,
            mockAnalysisRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var result = await service.ListAsync(CancellationToken.None);

        Assert.IsType<Outcome<List<CalibrationCurveView>>.Ok>(result);
        var ok = (Outcome<List<CalibrationCurveView>>.Ok)result;

        Assert.Equal(2, ok.Data.Count);

        var viewA = ok.Data[0];
        Assert.Equal("CurveA", viewA.Name);
        Assert.Equal("TemplateA", viewA.TemplateName);
        Assert.Equal("Inkerman", viewA.Site);
        Assert.Equal(2, viewA.Points.Count);
        Assert.Equal(0m, viewA.Points[0].XValue);
        Assert.Equal(10m, viewA.Points[1].XValue);
        Assert.Equal(100m, viewA.Points[1].YValue);

        var viewB = ok.Data[1];
        Assert.Equal("CurveB", viewB.Name);
        Assert.Equal("TemplateB", viewB.TemplateName);
        Assert.Equal("Proserpine", viewB.Site);
        Assert.Single(viewB.Points);
    }

    [Fact]
    public async Task DeactivateAsync_ControlLabAnalyst_ReturnsForbidden()
    {
        var mockRepository = new Mock<ICalibrationCurveRepository>();
        var mockAnalysisRepository = new Mock<IAnalysisRepository>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockCurrentUser.Setup(u => u.Role).Returns(Role.ControlLabAnalyst);

        var service = new CalibrationCurveService(
            mockRepository.Object,
            mockAnalysisRepository.Object,
            mockAuditLogger.Object,
            mockCurrentUser.Object,
            CreateTimeProvider());

        var result = await service.DeactivateAsync(1, CancellationToken.None);

        Assert.IsType<Outcome<bool>.Forbidden>(result);
    }
}
