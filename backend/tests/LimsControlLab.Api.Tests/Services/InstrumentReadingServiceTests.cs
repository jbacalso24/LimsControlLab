#pragma warning disable CA1707

using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.Domain.Services;
using LimsControlLab.SharedKernel.Enums;
using Moq;
using Xunit;

namespace LimsControlLab.Api.Tests.Services;

public sealed class InstrumentReadingServiceTests
{
    private readonly Mock<IInstrumentRepository> _mockRepository;
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly InstrumentReadingService _service;

    public InstrumentReadingServiceTests()
    {
        _mockRepository = new Mock<IInstrumentRepository>();
        _mockCurrentUser = new Mock<ICurrentUser>();
        _service = new InstrumentReadingService(_mockRepository.Object, _mockCurrentUser.Object, TimeProvider.System);
    }

    [Fact]
    public async Task ListByCurrentSiteAsync_ReturnsInstrumentsForUserSite()
    {
        _mockCurrentUser.Setup(u => u.Site).Returns(Site.Inkerman);
        var instruments = new List<Instrument>
        {
            new() { Id = 1, Name = "Instrument1", Site = Site.Inkerman, IsActive = true },
            new() { Id = 2, Name = "Instrument2", Site = Site.Inkerman, IsActive = true },
        };
        _mockRepository.Setup(r => r.ListBySiteAsync(Site.Inkerman, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instruments);

        var result = await _service.ListByCurrentSiteAsync(CancellationToken.None);

        Assert.True(result is Outcome<List<InstrumentDto>>.Ok);
        var ok = result as Outcome<List<InstrumentDto>>.Ok;
        Assert.NotNull(ok);
        Assert.Equal(2, ok.Data.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsInstrument()
    {
        _mockCurrentUser.Setup(u => u.Site).Returns(Site.Inkerman);
        var instrument = new Instrument { Id = 1, Name = "Test", Site = Site.Inkerman, IsActive = true };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        Assert.True(result is Outcome<InstrumentDto>.Ok);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNotFound()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instrument?)null);

        var result = await _service.GetByIdAsync(999, CancellationToken.None);

        Assert.True(result is Outcome<InstrumentDto>.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_WithDifferentSite_ReturnsForbidden()
    {
        _mockCurrentUser.Setup(u => u.Site).Returns(Site.Inkerman);
        var instrument = new Instrument { Id = 1, Name = "Test", Site = Site.Invicta, IsActive = true };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        Assert.True(result is Outcome<InstrumentDto>.Forbidden);
    }

    [Fact]
    public async Task CreateAsync_WithAnalystRole_ReturnsForbidden()
    {
        _mockCurrentUser.Setup(u => u.Role).Returns(Role.ControlLabAnalyst);

        var request = new CreateInstrumentRequest { Name = "NewInstrument", IsActive = true };
        var result = await _service.CreateAsync(request, CancellationToken.None);

        Assert.True(result is Outcome<InstrumentDto>.Forbidden);
    }

    [Fact]
    public async Task CreateAsync_WithCoordinatorRole_ReturnsOk()
    {
        _mockCurrentUser.Setup(u => u.Role).Returns(Role.LabCoordinator);
        _mockCurrentUser.Setup(u => u.Site).Returns(Site.Inkerman);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Instrument>(), It.IsAny<CancellationToken>()))
            .Callback<Instrument, CancellationToken>((i, _) => i.Id = 1)
            .Returns(Task.CompletedTask);

        var request = new CreateInstrumentRequest { Name = "NewInstrument", IsActive = true };
        var result = await _service.CreateAsync(request, CancellationToken.None);

        Assert.True(result is Outcome<InstrumentDto>.Ok);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Instrument>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithAnalystRole_ReturnsForbidden()
    {
        _mockCurrentUser.Setup(u => u.Role).Returns(Role.ControlLabAnalyst);

        var request = new UpdateInstrumentRequest
        {
            Name = "Updated",
            IsActive = true,
            RowVersion = "dGVzdA==",
        };
        var result = await _service.UpdateAsync(1, request, CancellationToken.None);

        Assert.True(result is Outcome<InstrumentDto>.Forbidden);
    }
}
