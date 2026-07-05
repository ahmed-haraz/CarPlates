using AutoMapper;
using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Application.History.Queries;
using MediatR;

namespace CarPlates.Application.History.Handlers;

public class GetScanHistoryHandler : IRequestHandler<GetScanHistoryQuery, PaginatedResult<ScanRecordListDto>>
{
    private readonly IScanRepository _scanRepository;
    private readonly IMapper _mapper;

    public GetScanHistoryHandler(IScanRepository scanRepository, IMapper mapper)
    {
        _scanRepository = scanRepository;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<ScanRecordListDto>> Handle(GetScanHistoryQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Domain.Entities.ScanRecord> scans;
        int totalCount;

        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            scans = await _scanRepository.SearchAsync(request.SearchQuery, cancellationToken);
            totalCount = scans.Count;
        }
        else if (request.StartDate.HasValue && request.EndDate.HasValue)
        {
            scans = await _scanRepository.GetByDateRangeAsync(request.StartDate.Value, request.EndDate.Value, cancellationToken);
            totalCount = scans.Count;
        }
        else
        {
            scans = await _scanRepository.GetAllAsync(cancellationToken);
            totalCount = scans.Count;
        }

        var paged = scans
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var dtos = _mapper.Map<IReadOnlyList<ScanRecordListDto>>(paged);
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return new PaginatedResult<ScanRecordListDto>(dtos, totalCount, request.Page, request.PageSize, totalPages);
    }
}
