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

public sealed class AnalysisTemplateServiceTests
{
    [Fact]
    public async Task CreateAsync_LabCoordinatorCreatesTemplate_ReturnsOkWithVersion1()
    {
        var mockRepo = new Mock<IAnalysisTemplateRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockCurrentUser.Setup(u => u.Role).Returns(Role.LabCoordinator);
        mockCurrentUser.Setup(u => u.UserId).Returns(1);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        mockAuditLogger.Setup(a => a.LogAsync(It.IsAny<AuditLogEntryRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new AnalysisTemplateService(mockRepo.Object, mockUnitOfWork.Object, mockCurrentUser.Object, mockAuditLogger.Object, TimeProvider.System);

        var request = new CreateTemplateRequest
        {
            Name = "TestTemplate",
            Site = Site.Inkerman,
            MinTolerance = 1m,
            MaxTolerance = 5m,
        };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.IsType<Outcome<AnalysisTemplateServiceDto>.Ok>(result);
        if (result is Outcome<AnalysisTemplateServiceDto>.Ok ok)
        {
            Assert.Equal(1, ok.Data.Version);
            Assert.Equal("TestTemplate", ok.Data.Name);
            Assert.False(ok.Data.IsRetired);
        }

        mockAuditLogger.Verify(a => a.LogAsync(It.IsAny<AuditLogEntryRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_AnalystAttempts_ReturnsForbidden()
    {
        var mockRepo = new Mock<IAnalysisTemplateRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockCurrentUser.Setup(u => u.Role).Returns(Role.ControlLabAnalyst);

        var service = new AnalysisTemplateService(mockRepo.Object, mockUnitOfWork.Object, mockCurrentUser.Object, mockAuditLogger.Object, TimeProvider.System);

        var request = new CreateTemplateRequest
        {
            Name = "TestTemplate",
            Site = Site.Inkerman,
        };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.IsType<Outcome<AnalysisTemplateServiceDto>.Forbidden>(result);
    }

    [Fact]
    public async Task UpdateAsync_LabCoordinatorModifies_CreatesVersion2()
    {
        var mockRepo = new Mock<IAnalysisTemplateRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        var version1 = new AnalysisTemplateVersion { Id = 1, TemplateId = 1, Version = 1, MinTolerance = 1m, MaxTolerance = 5m, CreatedAtUtc = DateTimeOffset.UtcNow };
        var template = new AnalysisTemplate
        {
            Id = 1,
            Name = "TestTemplate",
            Site = Site.Inkerman,
            CurrentVersionId = 1,
            CurrentVersion = version1,
            IsRetired = false,
            RowVersion = new byte[] { 1, 2, 3 },
        };

        mockCurrentUser.Setup(u => u.Role).Returns(Role.LabCoordinator);
        mockCurrentUser.Setup(u => u.UserId).Returns(1);
        mockRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(template);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        mockAuditLogger.Setup(a => a.LogAsync(It.IsAny<AuditLogEntryRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new AnalysisTemplateService(mockRepo.Object, mockUnitOfWork.Object, mockCurrentUser.Object, mockAuditLogger.Object, TimeProvider.System);

        var request = new UpdateTemplateRequest
        {
            Name = "UpdatedTemplate",
            MinTolerance = 2m,
            MaxTolerance = 10m,
            RowVersion = new byte[] { 1, 2, 3 },
        };

        var result = await service.UpdateAsync(1, request, CancellationToken.None);

        Assert.IsType<Outcome<AnalysisTemplateServiceDto>.Ok>(result);
        if (result is Outcome<AnalysisTemplateServiceDto>.Ok ok)
        {
            Assert.Equal(2, ok.Data.Version);
            Assert.Equal("UpdatedTemplate", ok.Data.Name);
        }
    }

    [Fact]
    public async Task RetireAsync_LabCoordinator_MarksAsRetired()
    {
        var mockRepo = new Mock<IAnalysisTemplateRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        var template = new AnalysisTemplate
        {
            Id = 1,
            Name = "TestTemplate",
            Site = Site.Inkerman,
            CurrentVersionId = 1,
            IsRetired = false,
        };

        mockCurrentUser.Setup(u => u.Role).Returns(Role.LabCoordinator);
        mockCurrentUser.Setup(u => u.UserId).Returns(1);
        mockRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(template);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        mockAuditLogger.Setup(a => a.LogAsync(It.IsAny<AuditLogEntryRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new AnalysisTemplateService(mockRepo.Object, mockUnitOfWork.Object, mockCurrentUser.Object, mockAuditLogger.Object, TimeProvider.System);

        var result = await service.RetireAsync(1, CancellationToken.None);

        Assert.IsType<Outcome<bool>.Ok>(result);
        mockAuditLogger.Verify(a => a.LogAsync(It.IsAny<AuditLogEntryRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
