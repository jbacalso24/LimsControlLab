namespace LimsControlLab.Api.Controllers;

public sealed record AuditLogDto
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

public sealed record AuditLogPageDto
{
    public required List<AuditLogDto> Items { get; init; }
    public required int Total { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
}
