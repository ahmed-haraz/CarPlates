namespace CarPlates.Application.Common.Interfaces;

public interface IBillApiService
{
    Task<BillApiResult> CreateBillAsync(CreateBillRequest request, CancellationToken cancellationToken = default);
    Task<BillSearchResult> SearchBillsAsync(string? search, int? dateFrom, int? dateTo, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<TodayStatsResult> GetTodayStatsAsync(CancellationToken cancellationToken = default);
    Task<BillDetailResult?> GetBillByIdAsync(long headerId, CancellationToken cancellationToken = default);
}

public record CreateBillRequest(
    int? BranchID,
    int? CustomerId,
    int? EngineerId,
    int? CarHeaderId,
    int? SalesRepId,
    int? StoreId,
    string? Notes,
    string? ReferenceNo,
    string? PlateNumber,
    string? Signature,
    IReadOnlyList<CreateBillLineRequest> Details,
    string? Vin = null,
    string? VehicleBrand = null,
    string? VehicleModel = null,
    string? VehicleTypeName = null,
    string? EngineTypeName = null,
    long? Mileage = null,
    int? VehicleYear = null,
    string? Color = null,
    string? PlateType = null,
    int? WorkLocationID = null,
    int? TechnicianID = null);

public record CreateBillLineRequest(
    string ItemBarCode,
    long ItemID,
    int? Package,
    double Qty,
    double Price,
    double? DetailDiscount1,
    double? DetailDiscount2,
    double? DetailDiscountR1,
    double? DetailDiscountR2,
    double? DetailTax,
    double? DetailTaxR,
    string? DetailNotes,
    double? Pkg2Qty = null,
    double? Pkg3Qty = null,
    double? Pkg1Price1 = null,
    double? Pkg2Price1 = null,
    double? Pkg3Price1 = null,
    double? Pkg1Price2 = null,
    double? Pkg2Price2 = null,
    double? Pkg3Price2 = null,
    double? OriginalPrice = null);

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
    string? ReferenceNo,
    string? PlateNumber,
    int? TransDate,
    string? CustomerName,
    string? Signature,
    string? WorkLocationName = null,
    string? TechnicianName = null,
    string? Color = null,
    string? PlateType = null);

public record BillDetailResult(
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
    string? ReferenceNo,
    string? PlateNumber,
    int? TransDate,
    string? CustomerName,
    string? Signature,
    string? WorkLocationName = null,
    string? TechnicianName = null,
    string? Color = null,
    string? PlateType = null,
    IReadOnlyList<BillLineItem>? Details = null);

public record BillLineItem(
    long DetailId,
    long ItemID,
    string ItemBarCode,
    int? Package,
    double Qty,
    double Price,
    double? DetailDiscount1 = null,
    double? DetailDiscount2 = null,
    double? DetailDiscountR1 = null,
    double? DetailDiscountR2 = null,
    double? DetailTax = null,
    double? DetailTaxR = null,
    double? Value = null,
    double? TransPkgQty1 = null,
    double? CostPrice = null,
    double? TransPkgPrice1 = null,
    double? WholeProfit = null,
    double? Pkg2Qty = null,
    double? Pkg3Qty = null,
    double? OriginalPrice = null,
    double? WholePrice = null,
    string? ItemName = null);
