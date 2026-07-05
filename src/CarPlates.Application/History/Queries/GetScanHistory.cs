using CarPlates.Application.Common.DTOs;
using MediatR;

namespace CarPlates.Application.History.Queries;

public record GetScanHistoryQuery(
    string? SearchQuery = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedResult<ScanRecordListDto>>;

public record PaginatedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
