using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Services;

/// <summary>
/// Derives schedule adherence (on track / due / overdue / missed) by comparing each active
/// Schedule's cadence against actual Analysis start times (BRD 6.2). Read-only.
/// </summary>
public sealed class ScheduleAdherenceService
{
    private const int LookbackPeriods = 6;

    private readonly IScheduleRepository _scheduleRepository;
    private readonly IUserRepository _userRepository;
    private readonly TimeProvider _clock;

    public ScheduleAdherenceService(
        IScheduleRepository scheduleRepository,
        IUserRepository userRepository,
        TimeProvider clock)
    {
        _scheduleRepository = scheduleRepository;
        _userRepository = userRepository;
        _clock = clock;
    }

    public async Task<Outcome<ScheduleAdherenceResult>> GetAdherenceAsync(Site site, CancellationToken ct)
    {
        var now = _clock.GetUtcNow();

        var schedules = (await _scheduleRepository.ListBySiteAsync(site, ct))
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToList();

        // Weekly is the longest cadence (7 days), so a lookback of LookbackPeriods weeks
        // safely covers the lookback window for Day and Shift schedules too.
        var sinceUtc = now - TimeSpan.FromDays(7 * LookbackPeriods);
        var markers = await _scheduleRepository.GetAnalysisMarkersAsync(site, sinceUtc, ct);

        var items = new List<ScheduleAdherenceItem>();

        foreach (var schedule in schedules)
        {
            var length = GetPeriodLength(schedule.ShiftPattern);
            var currentStart = GetCurrentPeriodStart(schedule.ShiftPattern, now);
            var currentEnd = currentStart + length;

            var matching = markers
                .Where(m => string.IsNullOrWhiteSpace(schedule.AnalysisType)
                    || string.Equals(m.TemplateName, schedule.AnalysisType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var currentCovered = matching.Any(m => m.StartedAtUtc >= currentStart && m.StartedAtUtc < currentEnd);

            var closedUncoveredStreak = 0;
            for (var i = 1; i <= LookbackPeriods; i++)
            {
                var periodStart = currentStart - (length * i);
                var periodEnd = currentStart - (length * (i - 1));
                var covered = matching.Any(m => m.StartedAtUtc >= periodStart && m.StartedAtUtc < periodEnd);
                if (covered)
                    break;

                closedUncoveredStreak++;
            }

            string status;
            int missedPeriods;

            if (currentCovered)
            {
                status = "OnTrack";
                missedPeriods = 0;
            }
            else if (closedUncoveredStreak >= 1)
            {
                status = "Missed";
                missedPeriods = closedUncoveredStreak;
            }
            else
            {
                var fractionElapsed = (now - currentStart) / length;
                status = fractionElapsed < 0.5 ? "Due" : "Overdue";
                missedPeriods = 0;
            }

            var lookbackStart = currentStart - (length * LookbackPeriods);
            var lastAnalysisAtUtc = matching
                .Where(m => m.StartedAtUtc >= lookbackStart && m.StartedAtUtc < currentEnd)
                .Select(m => (DateTimeOffset?)m.StartedAtUtc)
                .DefaultIfEmpty(null)
                .Max();

            string? assignedToUsername = null;
            if (schedule.AssignedToUserId.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(schedule.AssignedToUserId.Value, ct);
                assignedToUsername = user?.Username;
            }

            items.Add(new ScheduleAdherenceItem
            {
                ScheduleId = schedule.Id,
                Name = schedule.Name,
                AnalysisType = schedule.AnalysisType,
                ShiftPattern = schedule.ShiftPattern.ToString(),
                CadenceLabel = GetCadenceLabel(schedule.ShiftPattern),
                Status = status,
                AssignedToUserId = schedule.AssignedToUserId,
                AssignedToUsername = assignedToUsername,
                LastAnalysisAtUtc = lastAnalysisAtUtc,
                MissedPeriods = missedPeriods,
                CurrentPeriodStartUtc = currentStart,
                CurrentPeriodEndUtc = currentEnd,
            });
        }

        var summary = new AdherenceSummary
        {
            OnTrack = items.Count(i => i.Status == "OnTrack"),
            Due = items.Count(i => i.Status == "Due"),
            Overdue = items.Count(i => i.Status == "Overdue"),
            Missed = items.Count(i => i.Status == "Missed"),
            Total = items.Count,
        };

        return new Outcome<ScheduleAdherenceResult>.Ok(new ScheduleAdherenceResult
        {
            AsOfUtc = now,
            Summary = summary,
            Schedules = items,
        });
    }

    private static TimeSpan GetPeriodLength(ShiftPattern pattern) => pattern switch
    {
        ShiftPattern.Day => TimeSpan.FromDays(1),
        ShiftPattern.Shift => TimeSpan.FromHours(8),
        ShiftPattern.Weekly => TimeSpan.FromDays(7),
        _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Unknown shift pattern."),
    };

    private static DateTimeOffset GetCurrentPeriodStart(ShiftPattern pattern, DateTimeOffset now)
    {
        var utcDate = now.UtcDateTime.Date;

        return pattern switch
        {
            ShiftPattern.Day => new DateTimeOffset(utcDate, TimeSpan.Zero),
            ShiftPattern.Shift => new DateTimeOffset(utcDate, TimeSpan.Zero).AddHours((now.UtcDateTime.Hour / 8) * 8),
            ShiftPattern.Weekly => new DateTimeOffset(utcDate.AddDays(-(((int)utcDate.DayOfWeek + 6) % 7)), TimeSpan.Zero),
            _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Unknown shift pattern."),
        };
    }

    private static string GetCadenceLabel(ShiftPattern pattern) => pattern switch
    {
        ShiftPattern.Day => "Daily",
        ShiftPattern.Shift => "Every shift (8h)",
        ShiftPattern.Weekly => "Weekly",
        _ => pattern.ToString(),
    };
}

public sealed record ScheduleAdherenceResult
{
    public required DateTimeOffset AsOfUtc { get; init; }
    public required AdherenceSummary Summary { get; init; }
    public required List<ScheduleAdherenceItem> Schedules { get; init; }
}

public sealed record AdherenceSummary
{
    public required int OnTrack { get; init; }
    public required int Due { get; init; }
    public required int Overdue { get; init; }
    public required int Missed { get; init; }
    public required int Total { get; init; }
}

public sealed record ScheduleAdherenceItem
{
    public required int ScheduleId { get; init; }
    public required string Name { get; init; }
    public string? AnalysisType { get; init; }
    public required string ShiftPattern { get; init; }
    public required string CadenceLabel { get; init; }
    public required string Status { get; init; }
    public int? AssignedToUserId { get; init; }
    public string? AssignedToUsername { get; init; }
    public DateTimeOffset? LastAnalysisAtUtc { get; init; }
    public required int MissedPeriods { get; init; }
    public required DateTimeOffset CurrentPeriodStartUtc { get; init; }
    public required DateTimeOffset CurrentPeriodEndUtc { get; init; }
}
