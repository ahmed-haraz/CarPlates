namespace CarPlates.Application.Common.Interfaces;

public interface IBillApiService
{
    Task<BillApiResult> CreateBillAsync(CreateBillRequest request, CancellationToken cancellationToken = default);
    Task<BillSearchResult> SearchBillsAsync(string? search, int? dateFrom, int? dateTo, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<TodayStatsResult> GetTodayStatsAsync(CancellationToken cancellationToken = default);
}

public record CreateBillRequest(
    int? BranchID,
    int? CustomerId,
    int? EngineerId,
    int? CarHeaderId,
    string? Notes,
    string? RefrenceNo,
    IReadOnlyList<CreateBillLineRequest> Details);

public record CreateBillLineRequest(
    string ItemBarCode,
    long ItemID,
    int? Package,
    double Qty,
    double Price,
    double? DetailDiscount1,
    double? DetailTax,
    string? DetailNotes);

public record BillApiResult(bool Success, long? HeaderId, string? ErrorMessage);
public record BillSearchResult(bool Success, IReadOnlyList<BillApiItem> Items, int TotalCount, int Page, int TotalPages, string? ErrorMessage);
public record TodayStatsResult(bool Success, int TodayBills, double TodayTotal, string? ErrorMessage);

public record BillApiItem(
    long HeaderId,
    string? DocTransNo,
    int? BranchID,
    int? CustomerId,
    int? EngineerId,
    int? CarHeaderId,
    double Total,
    double NetTotal,
    double Paid,
    double Balance,
    byte? PayType,
    string? Notes,
    string? RefrenceNo,
    int? TransDate,
    string? CustomerName);
