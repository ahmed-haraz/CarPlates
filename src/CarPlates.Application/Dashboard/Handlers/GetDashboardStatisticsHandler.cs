using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Application.Dashboard.Queries;
using MediatR;

namespace CarPlates.Application.Dashboard.Handlers;

public class GetDashboardStatisticsHandler(IScanRepository scanRepository)
    : IRequestHandler<GetDashboardStatisticsQuery, DashboardStatisticsDto>
{
    private readonly IScanRepository _scanRepository = scanRepository;

    public Task<DashboardStatisticsDto> Handle(GetDashboardStatisticsQuery request, CancellationToken cancellationToken)
        => _scanRepository.GetStatisticsAsync(cancellationToken);
}

public class GetRecentScansHandler(IScanRepository scanRepository)
    : IRequestHandler<GetRecentScansQuery, IReadOnlyList<RecentScanDto>>
{
    private readonly IScanRepository _scanRepository = scanRepository;

    public Task<IReadOnlyList<RecentScanDto>> Handle(GetRecentScansQuery request, CancellationToken cancellationToken)
        => _scanRepository.GetRecentAsync(request.Count, cancellationToken);
}
