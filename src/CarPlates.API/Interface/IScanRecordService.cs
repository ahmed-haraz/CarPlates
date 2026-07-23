using CarPlates.API.Models;

namespace CarPlates.API.Interface;

public interface IScanRecordService
{
    Task<ScanRecordDto?> GetByIdAsync(int id);
    Task<PagedResult<ScanRecordDto>> GetAllAsync(string? plateNumber = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20, int? branchId = null, long? userId = null);
    Task<IReadOnlyList<RecentScanDto>> GetRecentAsync(int count = 10, int? branchId = null, long? userId = null);
    Task<ScanRecordDto> CreateAsync(ScanRecordCreateDto dto, string? userId = null);
    Task<DashboardStatisticsDto> GetStatisticsAsync(int? branchId = null, long? userId = null);
    Task<SyncBatchResponseDto> SyncBatchAsync(SyncBatchRequestDto dto, string? userId = null);
}