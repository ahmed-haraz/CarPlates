using CarPlates.API.Models;

namespace CarPlates.API.Interface;

public interface IBillService
{
    Task<BillDto> CreateAsync(CreateBillDto dto, string? userId, CancellationToken cancellationToken = default);

    Task<BillDto?> GetByIdAsync(long headerId, CancellationToken cancellationToken = default);

    Task<PagedResult<BillDto>> GetAllAsync(
        int branchId, int? customerId, int? carHeaderId,
        int page, int pageSize, CancellationToken cancellationToken = default);

    Task<PagedResult<BillDto>> SearchAsync(
        string? search, int? transDateFrom, int? transDateTo,
        int page, int pageSize, string? userId = null, int? branchId = null,
        CancellationToken cancellationToken = default);

    Task<(int todayBills, double todayTotal)> GetTodayStatsAsync(string? userId = null, int? branchId = null, CancellationToken cancellationToken = default);
}
