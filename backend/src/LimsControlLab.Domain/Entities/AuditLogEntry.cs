namespace LimsControlLab.Domain.Entities;

public sealed class AuditLogEntry
{
    public int Id { get; set; }
    public required int UserId { get; set; }
    public required string Role { get; set; }
    public required DateTimeOffset TimestampUtc { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public required int EntityId { get; set; }
    public string? BeforeValues { get; set; }
    public string? AfterValues { get; set; }
    public string? CorrelationId { get; set; }
}
