using CarPlates.API.Models;

namespace CarPlates.API.Interface;

public interface IBillService
{
    /// <summary>
    /// Saves a bill: one wh_TransHeader row plus its wh_TransDetails lines, in a single
    /// SaveChangesAsync call so the header and every detail commit together or not at all.
    /// </summary>
    Task<BillDto> CreateAsync(CreateBillDto dto, string? userId, CancellationToken cancellationToken = default);

    Task<BillDto?> GetByIdAsync(long headerId, CancellationToken cancellationToken = default);

    Task<PagedResult<BillDto>> GetAllAsync(
        int? branchId, int? customerId, int? carHeaderId,
        int page, int pageSize, CancellationToken cancellationToken = default);
}
