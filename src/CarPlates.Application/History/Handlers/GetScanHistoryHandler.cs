using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Application.History.Queries;
using MediatR;

namespace CarPlates.Application.History.Handlers;

public class GetScanHistoryHandler(IScanRepository scanRepository)
    : IRequestHandler<GetScanHistoryQuery, PaginatedResult<ScanRecordListDto>>
{
    private readonly IScanRepository _scanRepository = scanRepository;

    public async Task<PaginatedResult<ScanRecordListDto>> Handle(GetScanHistoryQuery request, CancellationToken cancellationToken)
    {
        var startDate = request.StartDate;
        var endDate = request.EndDate;
        var plateFilter = string.IsNullOrWhiteSpace(request.SearchQuery) ? null : request.SearchQuery;

        var scans = await _scanRepository.GetAllAsync(plateFilter, startDate, endDate, cancellationToken);
        var totalCount = scans.Count;

        var paged = scans
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new ScanRecordListDto(
                s.Id,
                s.PlateNumber,
                s.PlateType,
                s.Confidence,
                s.ScanTime,
                s.VehicleBrand,
                s.AccessStatus))
            .ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return new PaginatedResult<ScanRecordListDto>(paged, totalCount, request.Page, request.PageSize, totalPages);
    }
}
