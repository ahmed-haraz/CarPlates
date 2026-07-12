using AutoMapper;
using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Application.Dashboard.Queries;
using MediatR;

namespace CarPlates.Application.Dashboard.Handlers;

public class GetDashboardStatisticsHandler(
    IScanRepository scanRepository,
    IPendingUploadRepository pendingUploadRepository) : IRequestHandler<GetDashboardStatisticsQuery, DashboardStatisticsDto>
{
    private readonly IScanRepository _scanRepository = scanRepository;
    private readonly IPendingUploadRepository _pendingUploadRepository = pendingUploadRepository;

    public async Task<DashboardStatisticsDto> Handle(GetDashboardStatisticsQuery request, CancellationToken cancellationToken)
    {
        var total = await _scanRepository.GetTotalCountAsync(cancellationToken);
        var today = await _scanRepository.GetTodayCountAsync(cancellationToken);
        var pending = await _pendingUploadRepository.GetPendingCountAsync(cancellationToken);
        var failed = await _pendingUploadRepository.GetFailedAsync(cancellationToken);
        var allScans = await _scanRepository.GetAllAsync(cancellationToken);
        var unique = allScans.Select(s => s.PlateNumber).Distinct().Count();

        return new DashboardStatisticsDto(total, today, pending, failed.Count, unique);
    }
}

public class GetRecentScansHandler(IScanRepository scanRepository, IMapper mapper) : IRequestHandler<GetRecentScansQuery, IReadOnlyList<RecentScanDto>>
{
    private readonly IScanRepository _scanRepository = scanRepository;
    private readonly IMapper _mapper = mapper;

    public async Task<IReadOnlyList<RecentScanDto>> Handle(GetRecentScansQuery request, CancellationToken cancellationToken)
    {
        var scans = await _scanRepository.GetRecentAsync(request.Count, cancellationToken);
        return _mapper.Map<IReadOnlyList<RecentScanDto>>(scans);
    }
}
