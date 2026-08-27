#pragma warning disable CA1707

using Moq;
using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.Domain.Services;
using LimsControlLab.SharedKernel.Enums;
using Xunit;

namespace LimsControlLab.Api.Tests.Services;

public sealed class ResultComparisonServiceTests
{
    private static SearchResult MakeResult(
        int analysisId,
        decimal? value,
        DateTimeOffset? capturedAtUtc,
        string? unit = "%",
        string templateName = "Final Molasses Purity")
        => new()
        {
            AnalysisId = analysisId,
            SampleId = analysisId,
            SampleIdentifier = $"INV-{analysisId}",
            TemplateName = templateName,
            Site = Site.Inkerman,
            Status = LifecycleStatus.Completed,
            IsLocked = false,
            StartedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ReadingId = analysisId,
            TestId = analysisId,
            ReadingValue = value,
            ReadingUnit = unit,
            CapturedAtUtc = capturedAtUtc,
        };

    private static (Mock<ISearchRepository> Repo, Mock<IAnalysisTemplateRepository> Templates, Mock<ICurrentUser> User) CreateMocks()
    {
        var repo = new Mock<ISearchRepository>();
        var templates = new Mock<IAnalysisTemplateRepository>();
        var user = new Mock<ICurrentUser>();
        user.Setup(u => u.Site).Returns(Site.Inkerman);
        templates.Setup(t => t.ListBySiteAsync(It.IsAny<Site>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AnalysisTemplate>());
        return (repo, templates, user);
    }

    [Fact]
    public async Task CompareAsync_FiltersOutRowsWithNullValueOrNullCapturedAt()
    {
        var (repo, templates, user) = CreateMocks();
        var now = DateTimeOffset.UtcNow;
        var results = new List<SearchResult>
        {
            MakeResult(1, 10m, now),
            MakeResult(2, null, now),
            MakeResult(3, 10m, null),
        };
        repo.Setup(r => r.Search(It.IsAny<SearchFilter>())).Returns(results.AsQueryable());

        var service = new ResultComparisonService(repo.Object, templates.Object, user.Object);
        var outcome = await service.CompareAsync(new ResultComparisonQuery(), CancellationToken.None);

        var ok = Assert.IsType<Outcome<ResultComparisonResult>.Ok>(outcome);
        Assert.Single(ok.Data.Points);
        Assert.Equal(1, ok.Data.Points[0].AnalysisId);
    }

    [Fact]
    public async Task CompareAsync_OrdersPointsByCapturedAtUtcAscending()
    {
        var (repo, templates, user) = CreateMocks();
        var now = DateTimeOffset.UtcNow;
        var results = new List<SearchResult>
        {
            MakeResult(1, 10m, now),
            MakeResult(2, 20m, now.AddHours(-2)),
            MakeResult(3, 30m, now.AddHours(-1)),
        };
        repo.Setup(r => r.Search(It.IsAny<SearchFilter>())).Returns(results.AsQueryable());

        var service = new ResultComparisonService(repo.Object, templates.Object, user.Object);
        var outcome = await service.CompareAsync(new ResultComparisonQuery(), CancellationToken.None);

        var ok = Assert.IsType<Outcome<ResultComparisonResult>.Ok>(outcome);
        var orderedIds = ok.Data.Points.Select(p => p.AnalysisId).ToList();
        Assert.Equal(2, orderedIds[0]);
        Assert.Equal(3, orderedIds[1]);
        Assert.Equal(1, orderedIds[2]);
    }

    [Fact]
    public async Task CompareAsync_WithSingleDistinctUnit_ReturnsThatUnit()
    {
        var (repo, templates, user) = CreateMocks();
        var now = DateTimeOffset.UtcNow;
        var results = new List<SearchResult>
        {
            MakeResult(1, 10m, now, unit: "%"),
            MakeResult(2, 20m, now.AddHours(1), unit: "%"),
        };
        repo.Setup(r => r.Search(It.IsAny<SearchFilter>())).Returns(results.AsQueryable());

        var service = new ResultComparisonService(repo.Object, templates.Object, user.Object);
        var outcome = await service.CompareAsync(new ResultComparisonQuery(), CancellationToken.None);

        var ok = Assert.IsType<Outcome<ResultComparisonResult>.Ok>(outcome);
        Assert.Equal("%", ok.Data.Unit);
    }

    [Fact]
    public async Task CompareAsync_WithMixedUnits_ReturnsNullUnit()
    {
        var (repo, templates, user) = CreateMocks();
        var now = DateTimeOffset.UtcNow;
        var results = new List<SearchResult>
        {
            MakeResult(1, 10m, now, unit: "%"),
            MakeResult(2, 20m, now.AddHours(1), unit: "ppm"),
        };
        repo.Setup(r => r.Search(It.IsAny<SearchFilter>())).Returns(results.AsQueryable());

        var service = new ResultComparisonService(repo.Object, templates.Object, user.Object);
        var outcome = await service.CompareAsync(new ResultComparisonQuery(), CancellationToken.None);

        var ok = Assert.IsType<Outcome<ResultComparisonResult>.Ok>(outcome);
        Assert.Null(ok.Data.Unit);
    }

    [Fact]
    public async Task CompareAsync_WithSingleMatchingTemplate_ReturnsItsTolerance()
    {
        var (repo, templates, user) = CreateMocks();
        var now = DateTimeOffset.UtcNow;
        var results = new List<SearchResult>
        {
            MakeResult(1, 34.2m, now),
        };
        repo.Setup(r => r.Search(It.IsAny<SearchFilter>())).Returns(results.AsQueryable());
        templates.Setup(t => t.ListBySiteAsync(Site.Inkerman, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AnalysisTemplate>
            {
                new()
                {
                    Id = 1,
                    Name = "Final Molasses Purity",
                    Site = Site.Inkerman,
                    IsRetired = false,
                    MinTolerance = 98.0m,
                    MaxTolerance = 99.8m,
                },
            });

        var service = new ResultComparisonService(repo.Object, templates.Object, user.Object);
        var query = new ResultComparisonQuery { TemplateName = "final molasses purity" };
        var outcome = await service.CompareAsync(query, CancellationToken.None);

        var ok = Assert.IsType<Outcome<ResultComparisonResult>.Ok>(outcome);
        Assert.Equal(98.0m, ok.Data.ToleranceMin);
        Assert.Equal(99.8m, ok.Data.ToleranceMax);
    }

    [Fact]
    public async Task CompareAsync_WithoutTemplateName_ReturnsNullTolerance()
    {
        var (repo, templates, user) = CreateMocks();
        repo.Setup(r => r.Search(It.IsAny<SearchFilter>())).Returns(new List<SearchResult>().AsQueryable());

        var service = new ResultComparisonService(repo.Object, templates.Object, user.Object);
        var outcome = await service.CompareAsync(new ResultComparisonQuery(), CancellationToken.None);

        var ok = Assert.IsType<Outcome<ResultComparisonResult>.Ok>(outcome);
        Assert.Null(ok.Data.ToleranceMin);
        Assert.Null(ok.Data.ToleranceMax);
    }

    [Fact]
    public async Task CompareAsync_WithNoMatchingTemplate_ReturnsNullTolerance()
    {
        var (repo, templates, user) = CreateMocks();
        repo.Setup(r => r.Search(It.IsAny<SearchFilter>())).Returns(new List<SearchResult>().AsQueryable());
        templates.Setup(t => t.ListBySiteAsync(Site.Inkerman, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AnalysisTemplate>
            {
                new()
                {
                    Id = 1,
                    Name = "Something Else",
                    Site = Site.Inkerman,
                    IsRetired = false,
                    MinTolerance = 1m,
                    MaxTolerance = 2m,
                },
            });

        var service = new ResultComparisonService(repo.Object, templates.Object, user.Object);
        var query = new ResultComparisonQuery { TemplateName = "Final Molasses Purity" };
        var outcome = await service.CompareAsync(query, CancellationToken.None);

        var ok = Assert.IsType<Outcome<ResultComparisonResult>.Ok>(outcome);
        Assert.Null(ok.Data.ToleranceMin);
        Assert.Null(ok.Data.ToleranceMax);
    }

    [Fact]
    public async Task CompareAsync_WithEmptyResults_ReturnsEmptyPointsAndZeroCount()
    {
        var (repo, templates, user) = CreateMocks();
        repo.Setup(r => r.Search(It.IsAny<SearchFilter>())).Returns(new List<SearchResult>().AsQueryable());

        var service = new ResultComparisonService(repo.Object, templates.Object, user.Object);
        var outcome = await service.CompareAsync(new ResultComparisonQuery(), CancellationToken.None);

        var ok = Assert.IsType<Outcome<ResultComparisonResult>.Ok>(outcome);
        Assert.Empty(ok.Data.Points);
        Assert.Equal(0, ok.Data.TotalPoints);
        Assert.Null(ok.Data.Unit);
        Assert.Null(ok.Data.ToleranceMin);
        Assert.Null(ok.Data.ToleranceMax);
    }

    [Fact]
    public async Task CompareAsync_TotalPointsMatchesPointsCount()
    {
        var (repo, templates, user) = CreateMocks();
        var now = DateTimeOffset.UtcNow;
        var results = new List<SearchResult>
        {
            MakeResult(1, 10m, now),
            MakeResult(2, 20m, now.AddHours(1)),
            MakeResult(3, null, now.AddHours(2)),
        };
        repo.Setup(r => r.Search(It.IsAny<SearchFilter>())).Returns(results.AsQueryable());

        var service = new ResultComparisonService(repo.Object, templates.Object, user.Object);
        var outcome = await service.CompareAsync(new ResultComparisonQuery(), CancellationToken.None);

        var ok = Assert.IsType<Outcome<ResultComparisonResult>.Ok>(outcome);
        Assert.Equal(2, ok.Data.TotalPoints);
        Assert.Equal(ok.Data.TotalPoints, ok.Data.Points.Count);
    }
}
