#pragma warning disable CA1707

using Moq;
using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.Domain.Services;
using LimsControlLab.SharedKernel.Enums;
using Xunit;

namespace LimsControlLab.Api.Tests.Services;

public sealed class SearchServiceTests
{
    [Fact]
    public void Search_WithNoFilters_ReturnsAllResultsForUserSite()
    {
        var mockRepository = new Mock<ISearchRepository>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        var searchResults = new List<SearchResult>
        {
            new()
            {
                AnalysisId = 1,
                SampleId = 1,
                SampleIdentifier = "S001",
                TemplateName = "Template1",
                Site = Site.Inkerman,
                Status = LifecycleStatus.Completed,
                IsLocked = false,
                StartedAtUtc = DateTimeOffset.UtcNow,
                ReadingId = 1,
                TestId = 1,
            },
        };

        mockRepository.Setup(r => r.Search(It.IsAny<SearchFilter>()))
            .Returns(searchResults.AsQueryable());
        mockCurrentUser.Setup(u => u.Site).Returns(Site.Inkerman);

        var service = new SearchService(mockRepository.Object, mockCurrentUser.Object);
        var request = new SearchRequest();

        var result = service.Search(request).ToList();

        Assert.Single(result);
        Assert.Equal("Template1", result.First().TemplateName);
        mockRepository.Verify(r => r.Search(
            It.Is<SearchFilter>(f => f.Site == Site.Inkerman)), Times.Once);
    }

    [Fact]
    public void Search_WithTemplateNameFilter_PassesFilterToRepository()
    {
        var mockRepository = new Mock<ISearchRepository>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockRepository.Setup(r => r.Search(It.IsAny<SearchFilter>()))
            .Returns(new List<SearchResult>().AsQueryable());
        mockCurrentUser.Setup(u => u.Site).Returns(Site.Inkerman);

        var service = new SearchService(mockRepository.Object, mockCurrentUser.Object);
        var request = new SearchRequest { TemplateName = "Sugar" };

        _ = service.Search(request).ToList();

        mockRepository.Verify(r => r.Search(
            It.Is<SearchFilter>(f => f.TemplateName == "Sugar")), Times.Once);
    }

    [Fact]
    public void Search_WithTestIdFilter_PassesFilterToRepository()
    {
        var mockRepository = new Mock<ISearchRepository>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockRepository.Setup(r => r.Search(It.IsAny<SearchFilter>()))
            .Returns(new List<SearchResult>().AsQueryable());
        mockCurrentUser.Setup(u => u.Site).Returns(Site.Inkerman);

        var service = new SearchService(mockRepository.Object, mockCurrentUser.Object);
        var request = new SearchRequest { TestId = 42 };

        _ = service.Search(request).ToList();

        mockRepository.Verify(r => r.Search(
            It.Is<SearchFilter>(f => f.TestId == 42)), Times.Once);
    }

    [Fact]
    public void Search_WithInstrumentIdFilter_PassesFilterToRepository()
    {
        var mockRepository = new Mock<ISearchRepository>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockRepository.Setup(r => r.Search(It.IsAny<SearchFilter>()))
            .Returns(new List<SearchResult>().AsQueryable());
        mockCurrentUser.Setup(u => u.Site).Returns(Site.Inkerman);

        var service = new SearchService(mockRepository.Object, mockCurrentUser.Object);
        var request = new SearchRequest { InstrumentId = 5 };

        _ = service.Search(request).ToList();

        mockRepository.Verify(r => r.Search(
            It.Is<SearchFilter>(f => f.InstrumentId == 5)), Times.Once);
    }

    [Fact]
    public void Search_WithSampleIdentifierFilter_PassesFilterToRepository()
    {
        var mockRepository = new Mock<ISearchRepository>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockRepository.Setup(r => r.Search(It.IsAny<SearchFilter>()))
            .Returns(new List<SearchResult>().AsQueryable());
        mockCurrentUser.Setup(u => u.Site).Returns(Site.Inkerman);

        var service = new SearchService(mockRepository.Object, mockCurrentUser.Object);
        var request = new SearchRequest { SampleIdentifier = "S001" };

        _ = service.Search(request).ToList();

        mockRepository.Verify(r => r.Search(
            It.Is<SearchFilter>(f => f.SampleIdentifier == "S001")), Times.Once);
    }

    [Fact]
    public void Search_WithDateRangeFilter_PassesFilterToRepository()
    {
        var mockRepository = new Mock<ISearchRepository>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;

        mockRepository.Setup(r => r.Search(It.IsAny<SearchFilter>()))
            .Returns(new List<SearchResult>().AsQueryable());
        mockCurrentUser.Setup(u => u.Site).Returns(Site.Inkerman);

        var service = new SearchService(mockRepository.Object, mockCurrentUser.Object);
        var request = new SearchRequest { FromUtc = fromDate, ToUtc = toDate };

        _ = service.Search(request).ToList();

        mockRepository.Verify(r => r.Search(
            It.Is<SearchFilter>(f => f.FromUtc == fromDate && f.ToUtc == toDate)), Times.Once);
    }

    [Fact]
    public void Search_ScopesByCurrentUserSite()
    {
        var mockRepository = new Mock<ISearchRepository>();
        var mockCurrentUser = new Mock<ICurrentUser>();

        mockRepository.Setup(r => r.Search(It.IsAny<SearchFilter>()))
            .Returns(new List<SearchResult>().AsQueryable());
        mockCurrentUser.Setup(u => u.Site).Returns(Site.Proserpine);

        var service = new SearchService(mockRepository.Object, mockCurrentUser.Object);
        var request = new SearchRequest();

        _ = service.Search(request).ToList();

        mockRepository.Verify(r => r.Search(
            It.Is<SearchFilter>(f => f.Site == Site.Proserpine)), Times.Once);
    }
}
