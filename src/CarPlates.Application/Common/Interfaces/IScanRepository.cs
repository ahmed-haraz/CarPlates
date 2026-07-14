using CarPlates.Application.Common.DTOs;

namespace CarPlates.Application.Common.Interfaces;

// Backed entirely by the CarPlates API - there is no local database. Every
// call goes over the network, so callers should expect API-shaped exceptions
// (timeouts, connectivity failures) rather than local storage errors.
public interface IScanRepository
{
    Task<ScanRecordDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScanRecordDto>> GetAllAsync(
        string? plateNumber = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecentScanDto>> GetRecentAsync(int count, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScanRecordDto>> GetAllByPlateNumberAsync(string plateNumber, CancellationToken cancellationToken = default);

    Task<ScanRecordDto> CreateAsync(CreateScanRecordDto dto, CancellationToken cancellationToken = default);

    Task<DashboardStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default);
}
