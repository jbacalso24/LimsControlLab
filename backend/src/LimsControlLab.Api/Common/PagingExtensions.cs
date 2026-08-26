using Microsoft.EntityFrameworkCore;

namespace LimsControlLab.Api.Common;

public static class PagingExtensions
{
    private const int MaxPageSize = 500;

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var clampedPageSize = Math.Min(pageSize, MaxPageSize);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((pageNumber - 1) * clampedPageSize)
            .Take(clampedPageSize)
            .ToListAsync(ct);

        return new PagedResult<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = clampedPageSize,
            TotalCount = totalCount,
        };
    }
}
