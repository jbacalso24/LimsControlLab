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

public sealed class ScheduleServiceTests
{
    [Fact]
    public async Task CreateAsync_LabCoordinator_ReturnsOk()
    {
        var mockRepo = new Mock<IScheduleRepository>();
        var mockUserRepo = new Mock<IUserRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockCurrentUser.Setup(u => u.Role).Returns(Role.LabCoordinator);
        mockCurrentUser.Setup(u => u.UserId).Returns(1);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        mockAuditLogger.Setup(a => a.LogAsync(It.IsAny<AuditLogEntryRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new ScheduleService(mockRepo.Object, mockUserRepo.Object, mockUnitOfWork.Object, mockCurrentUser.Object, mockAuditLogger.Object, TimeProvider.System);

        var request = new CreateScheduleRequest
        {
            Name = "TestSchedule",
            Site = Site.Inkerman,
            ShiftPattern = ShiftPattern.Shift,
        };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.IsType<ScheduleServiceDto>(result switch
        {
            Outcome<ScheduleServiceDto>.Ok ok => ok.Data,
            _ => null!,
        });
    }

    [Fact]
    public async Task AssignAsync_AssignsToUser_LogsAudit()
    {
        var mockRepo = new Mock<IScheduleRepository>();
        var mockUserRepo = new Mock<IUserRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        var schedule = new Schedule
        {
            Id = 1,
            Name = "TestSchedule",
            Site = Site.Inkerman,
            ShiftPattern = ShiftPattern.Day,
            IsActive = true,
            AssignedToUserId = null,
        };

        var user = new User
        {
            Id = 2,
            Username = "analyst",
            PasswordHash = "hash",
            Role = Role.ControlLabAnalyst,
            Site = Site.Inkerman,
        };

        mockCurrentUser.Setup(u => u.Role).Returns(Role.LabCoordinator);
        mockCurrentUser.Setup(u => u.UserId).Returns(1);
        mockRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(schedule);
        mockUserRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        mockAuditLogger.Setup(a => a.LogAsync(It.IsAny<AuditLogEntryRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new ScheduleService(mockRepo.Object, mockUserRepo.Object, mockUnitOfWork.Object, mockCurrentUser.Object, mockAuditLogger.Object, TimeProvider.System);

        var result = await service.AssignAsync(1, 2, CancellationToken.None);

        Assert.IsType<Outcome<ScheduleServiceDto>.Ok>(result);
        mockAuditLogger.Verify(a => a.LogAsync(
            It.Is<AuditLogEntryRecord>(r => r.Action == "AssignSchedule"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
