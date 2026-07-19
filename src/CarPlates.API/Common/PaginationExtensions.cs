using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Common;

public static class PaginationExtensions
{
    // Hard ceiling so a client can't request page size 100000 and turn a "paginated"
    // endpoint back into an unbounded fetch-everything call.
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 20;

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<T>(items, totalCount, page, pageSize, totalPages);
    }
}
