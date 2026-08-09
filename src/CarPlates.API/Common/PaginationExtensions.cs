using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Common;

public static class PaginationExtensions
{
    // Hard ceiling so a client can't request page size 100000 and turn a "paginated"
    // endpoint back into an unbounded fetch-everything call.
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 20;

    // Passing pageSize = 0 is the explicit opt-in that returns ALL matching rows in a
    // single page (no Skip/Take). The returned PageSize stays 0 so the client can tell
    // "everything was fetched" apart from a normal page.
    public const int AllPageSize = 0;

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;

        var totalCount = await query.CountAsync(cancellationToken);

        IReadOnlyList<T> items;
        int effectivePageSize;

        if (pageSize == AllPageSize)
        {
            items = await query.ToListAsync(cancellationToken);
            effectivePageSize = AllPageSize;
        }
        else
        {
            effectivePageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
            items = await query
                .Skip((page - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .ToListAsync(cancellationToken);
        }

        var totalPages = totalCount == 0
            ? 0
            : effectivePageSize == AllPageSize
                ? 1
                : (int)Math.Ceiling(totalCount / (double)effectivePageSize);

        return new PagedResult<T>(items, totalCount, page, effectivePageSize, totalPages);
    }
}
