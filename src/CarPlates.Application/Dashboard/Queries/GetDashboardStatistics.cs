using CarPlates.Application.Common.DTOs;
using MediatR;

namespace CarPlates.Application.Dashboard.Queries;

public record GetDashboardStatisticsQuery : IRequest<DashboardStatisticsDto>;

public record GetRecentScansQuery(int Count = 10) : IRequest<IReadOnlyList<RecentScanDto>>;
