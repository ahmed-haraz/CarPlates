namespace CarPlates.API.Models;

// Every endpoint that returns a list from the database wraps it in this instead of a
// bare array, so the client always knows the total count and never has to guess whether
// it received everything.
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
