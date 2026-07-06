using CarPlates.API.Models.DTOs;

namespace CarPlates.API.Interface;

public interface IScanRecordService
{
    Task<ScanRecordDto?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<ScanRecordDto>> GetAllAsync(string? plateNumber = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<IReadOnlyList<RecentScanDto>> GetRecentAsync(int count = 10);
    Task<ScanRecordDto> CreateAsync(ScanRecordCreateDto dto, string? userId = null);
    Task<DashboardStatisticsDto> GetStatisticsAsync();
    Task<SyncBatchResponseDto> SyncBatchAsync(SyncBatchRequestDto dto, string? userId = null);
}