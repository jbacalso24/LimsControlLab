using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Repositories;

namespace LimsControlLab.Domain.Services;

/// <summary>
/// Read-side service for the audit trail (R3). Returns a page of recorded changes,
/// enriching each entry's raw user id with the username so the UI shows a name, not a number.
/// </summary>
public sealed class AuditTrailService
{
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;

    private readonly IAuditLogRepository _repository;
    private readonly IUserRepository _userRepository;

    public AuditTrailService(IAuditLogRepository repository, IUserRepository userRepository)
    {
        _repository = repository;
        _userRepository = userRepository;
    }

    public async Task<Outcome<AuditLogPageResult>> ListAsync(
        string? entityType,
        string? action,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        if (page < 1)
            page = 1;
        if (pageSize < 1)
            pageSize = DefaultPageSize;
        if (pageSize > MaxPageSize)
            pageSize = MaxPageSize;

        var skip = (page - 1) * pageSize;
        var pageData = await _repository.ListAsync(entityType, action, skip, pageSize, ct);

        var usernamesById = new Dictionary<int, string>();
        foreach (var userId in pageData.Items.Select(e => e.UserId).Distinct())
        {
            var user = await _userRepository.GetByIdAsync(userId, ct);
            if (user != null)
                usernamesById[userId] = user.Username;
        }

        var items = pageData.Items.Select(e => new AuditLogItem
        {
            Id = e.Id,
            UserId = e.UserId,
            Username = usernamesById.TryGetValue(e.UserId, out var name)
                ? name
                : e.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Role = e.Role,
            TimestampUtc = e.TimestampUtc,
            Action = e.Action,
            EntityType = e.EntityType,
            EntityId = e.EntityId,
            BeforeValues = e.BeforeValues,
            AfterValues = e.AfterValues,
            CorrelationId = e.CorrelationId,
        }).ToList();

        return new Outcome<AuditLogPageResult>.Ok(new AuditLogPageResult
        {
            Items = items,
            Total = pageData.Total,
            Page = page,
            PageSize = pageSize,
        });
    }
}

public sealed record AuditLogItem
{
    public required int Id { get; init; }
    public required int UserId { get; init; }
    public required string Username { get; init; }
    public required string Role { get; init; }
    public required DateTimeOffset TimestampUtc { get; init; }
    public required string Action { get; init; }
    public required string EntityType { get; init; }
    public required int EntityId { get; init; }
    public string? BeforeValues { get; init; }
    public string? AfterValues { get; init; }
    public string? CorrelationId { get; init; }
}

public sealed record AuditLogPageResult
{
    public required List<AuditLogItem> Items { get; init; }
    public required int Total { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
}
