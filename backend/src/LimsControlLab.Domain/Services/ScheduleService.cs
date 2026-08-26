using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Auditing;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Services;

public sealed class ScheduleService
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly TimeProvider _clock;

    public ScheduleService(
        IScheduleRepository scheduleRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IAuditLogger auditLogger,
        TimeProvider clock)
    {
        _scheduleRepository = scheduleRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _clock = clock;
    }

    public async Task<Outcome<ScheduleServiceDto>> CreateAsync(
        CreateScheduleRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_currentUser.Role != Role.LabCoordinator)
            return new Outcome<ScheduleServiceDto>.Forbidden("Only Lab Coordinators can create schedules.");

        var now = _clock.GetUtcNow();

        var schedule = new Schedule
        {
            Name = request.Name,
            Site = request.Site,
            AnalysisType = request.AnalysisType,
            ShiftPattern = request.ShiftPattern,
            RecurrencePattern = request.RecurrencePattern,
            ExclusionRules = request.ExclusionRules,
            AssignedToUserId = request.AssignedToUserId,
            IsActive = true,
        };

        _scheduleRepository.Add(schedule);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(new AuditLogEntryRecord
        {
            UserId = _currentUser.UserId,
            Role = _currentUser.Role.ToString(),
            TimestampUtc = now,
            Action = "CreateSchedule",
            EntityType = nameof(Schedule),
            EntityId = schedule.Id,
            AfterValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                schedule.Name,
                schedule.Site,
                schedule.IsActive,
            }),
        }, ct);

        return new Outcome<ScheduleServiceDto>.Ok(MapToDto(schedule));
    }

    public async Task<Outcome<ScheduleServiceDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(id, ct);

        if (schedule == null)
            return new Outcome<ScheduleServiceDto>.NotFound($"Schedule {id} not found.");

        return new Outcome<ScheduleServiceDto>.Ok(MapToDto(schedule));
    }

    public async Task<Outcome<ScheduleServiceDto>> UpdateAsync(
        int id,
        UpdateScheduleRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_currentUser.Role != Role.LabCoordinator)
            return new Outcome<ScheduleServiceDto>.Forbidden("Only Lab Coordinators can create schedules.");

        var schedule = await _scheduleRepository.GetByIdAsync(id, ct);

        if (schedule == null)
            return new Outcome<ScheduleServiceDto>.NotFound($"Schedule {id} not found.");

        if (!schedule.RowVersion.SequenceEqual(request.RowVersion))
            return new Outcome<ScheduleServiceDto>.Conflict(
                "Schedule was modified by another user. Please reload and try again.",
                Convert.ToBase64String(schedule.RowVersion));

        var now = _clock.GetUtcNow();

        schedule.Name = request.Name;
        schedule.AnalysisType = request.AnalysisType;
        schedule.ShiftPattern = request.ShiftPattern;
        schedule.RecurrencePattern = request.RecurrencePattern;
        schedule.ExclusionRules = request.ExclusionRules;
        schedule.AssignedToUserId = request.AssignedToUserId;
        schedule.IsActive = request.IsActive;

        _scheduleRepository.Update(schedule);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(new AuditLogEntryRecord
        {
            UserId = _currentUser.UserId,
            Role = _currentUser.Role.ToString(),
            TimestampUtc = now,
            Action = "UpdateSchedule",
            EntityType = nameof(Schedule),
            EntityId = schedule.Id,
            AfterValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                schedule.Name,
                schedule.IsActive,
            }),
        }, ct);

        return new Outcome<ScheduleServiceDto>.Ok(MapToDto(schedule));
    }

    public async Task<Outcome<bool>> DeleteAsync(int id, CancellationToken ct)
    {
        if (_currentUser.Role != Role.LabCoordinator)
            return new Outcome<bool>.Forbidden("Only Lab Coordinators can delete schedules.");

        var schedule = await _scheduleRepository.GetByIdAsync(id, ct);

        if (schedule == null)
            return new Outcome<bool>.NotFound($"Schedule {id} not found.");

        var now = _clock.GetUtcNow();

        _scheduleRepository.Remove(schedule);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(new AuditLogEntryRecord
        {
            UserId = _currentUser.UserId,
            Role = _currentUser.Role.ToString(),
            TimestampUtc = now,
            Action = "DeleteSchedule",
            EntityType = nameof(Schedule),
            EntityId = id,
        }, ct);

        return new Outcome<bool>.Ok(true);
    }

    public async Task<Outcome<List<ScheduleServiceDto>>> ListAsync(Site site, CancellationToken ct)
    {
        var schedules = await _scheduleRepository.ListBySiteAsync(site, ct);
        return new Outcome<List<ScheduleServiceDto>>.Ok(schedules.Select(MapToDto).ToList());
    }

    public async Task<Outcome<ScheduleServiceDto>> AssignAsync(
        int scheduleId,
        int userId,
        CancellationToken ct)
    {
        if (_currentUser.Role != Role.LabCoordinator)
            return new Outcome<ScheduleServiceDto>.Forbidden("Only Lab Coordinators can create schedules.");

        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId, ct);

        if (schedule == null)
            return new Outcome<ScheduleServiceDto>.NotFound($"Schedule {scheduleId} not found.");

        var user = await _userRepository.GetByIdAsync(userId, ct);

        if (user == null)
            return new Outcome<ScheduleServiceDto>.NotFound($"User {userId} not found.");

        var now = _clock.GetUtcNow();
        var previousAssignee = schedule.AssignedToUserId;

        schedule.AssignedToUserId = userId;
        _scheduleRepository.Update(schedule);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(new AuditLogEntryRecord
        {
            UserId = _currentUser.UserId,
            Role = _currentUser.Role.ToString(),
            TimestampUtc = now,
            Action = "AssignSchedule",
            EntityType = nameof(Schedule),
            EntityId = scheduleId,
            BeforeValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                AssignedToUserId = previousAssignee,
            }),
            AfterValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                AssignedToUserId = userId,
            }),
        }, ct);

        return new Outcome<ScheduleServiceDto>.Ok(MapToDto(schedule));
    }

    private static ScheduleServiceDto MapToDto(Schedule schedule) => new()
    {
        Id = schedule.Id,
        Name = schedule.Name,
        Site = schedule.Site.ToString(),
        AnalysisType = schedule.AnalysisType,
        ShiftPattern = schedule.ShiftPattern.ToString(),
        RecurrencePattern = schedule.RecurrencePattern,
        ExclusionRules = schedule.ExclusionRules,
        AssignedToUserId = schedule.AssignedToUserId,
        IsActive = schedule.IsActive,
        RowVersion = schedule.RowVersion,
    };
}

public sealed record CreateScheduleRequest
{
    public required Site Site { get; init; }
    public required string Name { get; init; }
    public string? AnalysisType { get; init; }
    public required ShiftPattern ShiftPattern { get; init; }
    public string? RecurrencePattern { get; init; }
    public string? ExclusionRules { get; init; }
    public int? AssignedToUserId { get; init; }
}

public sealed record UpdateScheduleRequest
{
    public required string Name { get; init; }
    public string? AnalysisType { get; init; }
    public required ShiftPattern ShiftPattern { get; init; }
    public string? RecurrencePattern { get; init; }
    public string? ExclusionRules { get; init; }
    public int? AssignedToUserId { get; init; }
    public required bool IsActive { get; init; }
    public required byte[] RowVersion { get; init; }
}

public sealed record ScheduleServiceDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Site { get; init; }
    public string? AnalysisType { get; init; }
    public required string ShiftPattern { get; init; }
    public string? RecurrencePattern { get; init; }
    public string? ExclusionRules { get; init; }
    public int? AssignedToUserId { get; init; }
    public required bool IsActive { get; init; }
    public required byte[] RowVersion { get; init; }
}
