#pragma warning disable CA1707

using Moq;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.Domain.Services;
using LimsControlLab.SharedKernel.Enums;
using Xunit;

namespace LimsControlLab.Api.Tests.Services;

public sealed class ScheduleAdherenceServiceTests
{
    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FakeTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }

    private static Schedule MakeSchedule(
        int id,
        string name,
        ShiftPattern pattern,
        string? analysisType = "Sugar Pol",
        bool isActive = true,
        int? assignedToUserId = null) => new()
    {
        Id = id,
        Name = name,
        Site = Site.Invicta,
        AnalysisType = analysisType,
        ShiftPattern = pattern,
        IsActive = isActive,
        AssignedToUserId = assignedToUserId,
    };

    private static ScheduleAdherenceService BuildService(
        DateTimeOffset now,
        List<Schedule> schedules,
        List<AnalysisAdherenceMarker> markers,
        out Mock<IScheduleRepository> mockScheduleRepository)
    {
        mockScheduleRepository = new Mock<IScheduleRepository>();
        mockScheduleRepository.Setup(r => r.ListBySiteAsync(Site.Invicta, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedules);
        mockScheduleRepository.Setup(r => r.GetAnalysisMarkersAsync(Site.Invicta, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(markers);

        var mockUserRepository = new Mock<IUserRepository>();

        return new ScheduleAdherenceService(mockScheduleRepository.Object, mockUserRepository.Object, new FakeTimeProvider(now));
    }

    [Fact]
    public async Task GetAdherenceAsync_AnalysisInCurrentDayPeriod_ReturnsOnTrack()
    {
        // Tuesday 2026-08-25 10:00 UTC -> current Day period is [2026-08-25T00:00, 2026-08-26T00:00)
        var now = new DateTimeOffset(2026, 8, 25, 10, 0, 0, TimeSpan.Zero);
        var schedule = MakeSchedule(1, "Sugar Pol Day", ShiftPattern.Day);
        var markers = new List<AnalysisAdherenceMarker>
        {
            new("Sugar Pol", now.AddHours(-1)),
        };

        var service = BuildService(now, [schedule], markers, out _);

        var result = await service.GetAdherenceAsync(Site.Invicta, CancellationToken.None);

        var ok = Assert.IsType<Outcome<ScheduleAdherenceResult>.Ok>(result);
        var item = Assert.Single(ok.Data.Schedules);
        Assert.Equal("OnTrack", item.Status);
        Assert.Equal(0, item.MissedPeriods);
        Assert.Equal(1, ok.Data.Summary.OnTrack);
        Assert.Equal(1, ok.Data.Summary.Total);
    }

    [Fact]
    public async Task GetAdherenceAsync_PreviousPeriodCoveredCurrentEmptyEarly_ReturnsDue()
    {
        // 2 hours into the Day period (fraction elapsed = 2/24 < 0.5), previous day covered
        var now = new DateTimeOffset(2026, 8, 25, 2, 0, 0, TimeSpan.Zero);
        var schedule = MakeSchedule(1, "Sugar Pol Day", ShiftPattern.Day);
        var previousPeriodStart = new DateTimeOffset(2026, 8, 24, 0, 0, 0, TimeSpan.Zero);
        var markers = new List<AnalysisAdherenceMarker>
        {
            new("Sugar Pol", previousPeriodStart.AddHours(5)),
        };

        var service = BuildService(now, [schedule], markers, out _);

        var result = await service.GetAdherenceAsync(Site.Invicta, CancellationToken.None);

        var ok = Assert.IsType<Outcome<ScheduleAdherenceResult>.Ok>(result);
        var item = Assert.Single(ok.Data.Schedules);
        Assert.Equal("Due", item.Status);
        Assert.Equal(0, item.MissedPeriods);
    }

    [Fact]
    public async Task GetAdherenceAsync_PreviousPeriodCoveredCurrentEmptyLate_ReturnsOverdue()
    {
        // 20 hours into the Day period (fraction elapsed = 20/24 >= 0.5), previous day covered
        var now = new DateTimeOffset(2026, 8, 25, 20, 0, 0, TimeSpan.Zero);
        var schedule = MakeSchedule(1, "Sugar Pol Day", ShiftPattern.Day);
        var previousPeriodStart = new DateTimeOffset(2026, 8, 24, 0, 0, 0, TimeSpan.Zero);
        var markers = new List<AnalysisAdherenceMarker>
        {
            new("Sugar Pol", previousPeriodStart.AddHours(5)),
        };

        var service = BuildService(now, [schedule], markers, out _);

        var result = await service.GetAdherenceAsync(Site.Invicta, CancellationToken.None);

        var ok = Assert.IsType<Outcome<ScheduleAdherenceResult>.Ok>(result);
        var item = Assert.Single(ok.Data.Schedules);
        Assert.Equal("Overdue", item.Status);
        Assert.Equal(0, item.MissedPeriods);
    }

    [Fact]
    public async Task GetAdherenceAsync_TwoClosedUncoveredPeriods_ReturnsMissedWithCorrectCount()
    {
        // Current period 2026-08-25, prior two days (24th, 23rd) uncovered, 22nd covered.
        var now = new DateTimeOffset(2026, 8, 25, 10, 0, 0, TimeSpan.Zero);
        var schedule = MakeSchedule(1, "Sugar Pol Day", ShiftPattern.Day);
        var markers = new List<AnalysisAdherenceMarker>
        {
            new("Sugar Pol", new DateTimeOffset(2026, 8, 22, 5, 0, 0, TimeSpan.Zero)),
        };

        var service = BuildService(now, [schedule], markers, out _);

        var result = await service.GetAdherenceAsync(Site.Invicta, CancellationToken.None);

        var ok = Assert.IsType<Outcome<ScheduleAdherenceResult>.Ok>(result);
        var item = Assert.Single(ok.Data.Schedules);
        Assert.Equal("Missed", item.Status);
        Assert.Equal(2, item.MissedPeriods);
        Assert.Equal(1, ok.Data.Summary.Missed);
    }

    [Fact]
    public async Task GetAdherenceAsync_InactiveSchedule_IsExcluded()
    {
        var now = new DateTimeOffset(2026, 8, 25, 10, 0, 0, TimeSpan.Zero);
        var activeSchedule = MakeSchedule(1, "Active", ShiftPattern.Day);
        var inactiveSchedule = MakeSchedule(2, "Inactive", ShiftPattern.Day, isActive: false);
        var markers = new List<AnalysisAdherenceMarker>
        {
            new("Sugar Pol", now.AddHours(-1)),
        };

        var service = BuildService(now, [activeSchedule, inactiveSchedule], markers, out _);

        var result = await service.GetAdherenceAsync(Site.Invicta, CancellationToken.None);

        var ok = Assert.IsType<Outcome<ScheduleAdherenceResult>.Ok>(result);
        Assert.Single(ok.Data.Schedules);
        Assert.Equal("Active", ok.Data.Schedules[0].Name);
        Assert.Equal(1, ok.Data.Summary.Total);
    }

    [Fact]
    public async Task GetAdherenceAsync_NullAnalysisType_MatchesAnyTemplate()
    {
        var now = new DateTimeOffset(2026, 8, 25, 10, 0, 0, TimeSpan.Zero);
        var schedule = MakeSchedule(1, "Any Analysis", ShiftPattern.Day, analysisType: null);
        var markers = new List<AnalysisAdherenceMarker>
        {
            new("Some Unrelated Template", now.AddHours(-1)),
        };

        var service = BuildService(now, [schedule], markers, out _);

        var result = await service.GetAdherenceAsync(Site.Invicta, CancellationToken.None);

        var ok = Assert.IsType<Outcome<ScheduleAdherenceResult>.Ok>(result);
        var item = Assert.Single(ok.Data.Schedules);
        Assert.Equal("OnTrack", item.Status);
    }

    [Fact]
    public async Task GetAdherenceAsync_MixOfStatuses_SummaryCountsAreCorrect()
    {
        var now = new DateTimeOffset(2026, 8, 25, 10, 0, 0, TimeSpan.Zero);

        var onTrack = MakeSchedule(1, "OnTrack", ShiftPattern.Day, analysisType: "TypeA");
        var missed = MakeSchedule(2, "Missed", ShiftPattern.Day, analysisType: "TypeB");
        var neverCovered = MakeSchedule(3, "NeverCovered", ShiftPattern.Day, analysisType: "TypeC");

        var markers = new List<AnalysisAdherenceMarker>
        {
            new("TypeA", now.AddHours(-1)),
            new("TypeB", new DateTimeOffset(2026, 8, 22, 5, 0, 0, TimeSpan.Zero)),
        };

        var service = BuildService(now, [onTrack, missed, neverCovered], markers, out _);

        var result = await service.GetAdherenceAsync(Site.Invicta, CancellationToken.None);

        var ok = Assert.IsType<Outcome<ScheduleAdherenceResult>.Ok>(result);
        Assert.Equal(3, ok.Data.Summary.Total);
        Assert.Equal(1, ok.Data.Summary.OnTrack);
        Assert.Equal(2, ok.Data.Summary.Missed);
    }

    [Fact]
    public async Task GetAdherenceAsync_WeeklyPattern_AnchorsToMondayNotCalendarProximity()
    {
        // Thursday 2026-08-27, current week starts Monday 2026-08-24T00:00 UTC.
        var now = new DateTimeOffset(2026, 8, 27, 10, 0, 0, TimeSpan.Zero);
        var schedule = MakeSchedule(1, "Weekly QA", ShiftPattern.Weekly);
        var markers = new List<AnalysisAdherenceMarker>
        {
            // Falls on the Monday anchor of the current week, well before "now".
            new("Sugar Pol", new DateTimeOffset(2026, 8, 24, 6, 0, 0, TimeSpan.Zero)),
        };

        var service = BuildService(now, [schedule], markers, out _);

        var result = await service.GetAdherenceAsync(Site.Invicta, CancellationToken.None);

        var ok = Assert.IsType<Outcome<ScheduleAdherenceResult>.Ok>(result);
        var item = Assert.Single(ok.Data.Schedules);
        Assert.Equal("OnTrack", item.Status);
        Assert.Equal("Weekly", item.CadenceLabel);
        Assert.Equal(new DateTimeOffset(2026, 8, 24, 0, 0, 0, TimeSpan.Zero), item.CurrentPeriodStartUtc);
        Assert.Equal(new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero), item.CurrentPeriodEndUtc);
    }

    [Fact]
    public async Task GetAdherenceAsync_DayPatternBoundary_MarkerJustBeforeMidnightDoesNotCoverNextDay()
    {
        // Current Day period is [2026-08-25T00:00, 2026-08-26T00:00). A marker one minute
        // before that boundary belongs to the previous period, not the current one.
        var now = new DateTimeOffset(2026, 8, 25, 1, 0, 0, TimeSpan.Zero);
        var schedule = MakeSchedule(1, "Sugar Pol Day", ShiftPattern.Day);
        var markers = new List<AnalysisAdherenceMarker>
        {
            new("Sugar Pol", new DateTimeOffset(2026, 8, 24, 23, 59, 0, TimeSpan.Zero)),
        };

        var service = BuildService(now, [schedule], markers, out _);

        var result = await service.GetAdherenceAsync(Site.Invicta, CancellationToken.None);

        var ok = Assert.IsType<Outcome<ScheduleAdherenceResult>.Ok>(result);
        var item = Assert.Single(ok.Data.Schedules);
        Assert.NotEqual("OnTrack", item.Status);
    }
}
