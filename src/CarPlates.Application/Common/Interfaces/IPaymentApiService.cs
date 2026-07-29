namespace CarPlates.Application.Common.Interfaces;

public interface IPaymentApiService
{
    Task<PayBillApiResult> PayAsync(PayBillApiRequest request, CancellationToken cancellationToken = default);
    Task<ReceiptApiResult?> GetReceiptAsync(long headerId, CancellationToken cancellationToken = default);
}

public record PaymentDetailItem(byte PayType, double Amount);

public record PayBillApiRequest(
    long HeaderId,
    IReadOnlyList<PaymentDetailItem> Payments,
    string? Notes);

public record PayBillApiResult(
    bool Success,
    string? Message,
    string? ReceiptNo,
    double PaidAmount,
    double Balance);

public record ReceiptApiResult(
    string? ReceiptNo,
    long HeaderId,
    string? DocTransNo,
    int? TransDate,
    string? CustomerName,
    string? ReferenceNo,
    string? PlateNumber,
    double Total,
    double NetTotal,
    double Paid,
    double Balance,
    byte? PayType,
    string? WorkLocationName = null,
    string? TechnicianName = null,
    string? Color = null,
    string? PlateType = null,
    IReadOnlyList<PaymentDetailItem>? Payments = null,
    IReadOnlyList<BillDetailApiItem>? Details = null);

public record BillDetailApiItem(
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
    double? WholePriceProfit = null,
    double? Pkg2Qty = null,
    double? Pkg3Qty = null,
    double? OriginalPrice = null,
    double? WholePrice = null,
    string? ItemName = null);
