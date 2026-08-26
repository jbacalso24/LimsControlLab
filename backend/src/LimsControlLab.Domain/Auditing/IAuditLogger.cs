namespace LimsControlLab.Domain.Auditing;

public interface IAuditLogger
{
    Task LogAsync(AuditLogEntryRecord entry, CancellationToken ct = default);
}

public sealed record AuditLogEntryRecord
{
    public required int UserId { get; init; }
    public required string Role { get; init; }
    public required DateTimeOffset TimestampUtc { get; init; }
    public required string Action { get; init; }
    public required string EntityType { get; init; }
    public required int EntityId { get; init; }
    public string? BeforeValues { get; init; }
    public string? AfterValues { get; init; }
    public string? CorrelationId { get; init; }
}
