namespace LimsControlLab.Api.Common;

public sealed record PagedResult<T>
{
    public required List<T> Items { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
}
