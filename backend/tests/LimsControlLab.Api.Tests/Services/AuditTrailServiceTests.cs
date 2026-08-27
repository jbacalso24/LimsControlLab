#pragma warning disable CA1707

using Moq;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.Domain.Services;
using LimsControlLab.SharedKernel.Enums;
using Xunit;

namespace LimsControlLab.Api.Tests.Services;

public sealed class AuditTrailServiceTests
{
    private static AuditLogEntry Entry(int id, int userId, string action = "ReadingCaptured", string entityType = "Reading") =>
        new()
        {
            Id = id,
            UserId = userId,
            Role = "ControlLabAnalyst",
            TimestampUtc = DateTimeOffset.UtcNow,
            Action = action,
            EntityType = entityType,
            EntityId = id,
            AfterValues = "Value: 99",
        };

    [Fact]
    public async Task ListAsync_EnrichesUsernameWithIdFallback()
    {
        var mockRepo = new Mock<IAuditLogRepository>();
        var mockUsers = new Mock<IUserRepository>();

        mockRepo
            .Setup(r => r.ListAsync(null, null, 0, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuditLogPage { Items = new[] { Entry(1, 7), Entry(2, 999) }, Total = 2 });
        mockUsers
            .Setup(u => u.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 7, Username = "inkerman_analyst", PasswordHash = "h", Role = Role.ControlLabAnalyst, Site = Site.Inkerman });
        mockUsers
            .Setup(u => u.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = new AuditTrailService(mockRepo.Object, mockUsers.Object);

        var result = await service.ListAsync(null, null, 1, 25, CancellationToken.None);

        var ok = Assert.IsType<Outcome<AuditLogPageResult>.Ok>(result);
        Assert.Equal(2, ok.Data.Total);
        Assert.Equal("inkerman_analyst", ok.Data.Items.First(i => i.Id == 1).Username);
        Assert.Equal("999", ok.Data.Items.First(i => i.Id == 2).Username);
    }

    [Fact]
    public async Task ListAsync_PassesFiltersAndComputesSkipFromPage()
    {
        var mockRepo = new Mock<IAuditLogRepository>();
        var mockUsers = new Mock<IUserRepository>();
        mockRepo
            .Setup(r => r.ListAsync("Reading", "ReadingCaptured", 20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuditLogPage { Items = Array.Empty<AuditLogEntry>(), Total = 0 })
            .Verifiable();

        var service = new AuditTrailService(mockRepo.Object, mockUsers.Object);

        // page 3, pageSize 10 -> skip 20
        var result = await service.ListAsync("Reading", "ReadingCaptured", 3, 10, CancellationToken.None);

        Assert.IsType<Outcome<AuditLogPageResult>.Ok>(result);
        mockRepo.Verify();
    }

    [Fact]
    public async Task ListAsync_ClampsInvalidPagingToSafeDefaults()
    {
        var mockRepo = new Mock<IAuditLogRepository>();
        var mockUsers = new Mock<IUserRepository>();
        // page < 1 -> 1 (skip 0); pageSize > 100 -> 100
        mockRepo
            .Setup(r => r.ListAsync(null, null, 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuditLogPage { Items = Array.Empty<AuditLogEntry>(), Total = 0 })
            .Verifiable();

        var service = new AuditTrailService(mockRepo.Object, mockUsers.Object);

        var result = await service.ListAsync(null, null, 0, 5000, CancellationToken.None);

        var ok = Assert.IsType<Outcome<AuditLogPageResult>.Ok>(result);
        Assert.Equal(1, ok.Data.Page);
        Assert.Equal(100, ok.Data.PageSize);
        mockRepo.Verify();
    }
}
