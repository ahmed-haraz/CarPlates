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

        var scans = await _scanRepository.GetAllAsync(
            plateFilter, startDate, endDate, request.Page, request.PageSize, cancellationToken);

        var items = scans.Items
            .Select(s => new ScanRecordListDto(
                s.Id,
                s.PlateNumber,
                s.PlateType,
                s.Confidence,
                s.ScanTime,
                s.VehicleBrand,
                s.AccessStatus))
            .ToList();

        return new PaginatedResult<ScanRecordListDto>(items, scans.TotalCount, scans.Page, scans.PageSize, scans.TotalPages);
    }
}
