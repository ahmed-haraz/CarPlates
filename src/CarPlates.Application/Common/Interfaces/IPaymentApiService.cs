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
    double Total,
    double NetTotal,
    double Paid,
    double Balance,
    byte? PayType,
    IReadOnlyList<PaymentDetailItem> Payments,
    IReadOnlyList<BillDetailApiItem> Details);

public record BillDetailApiItem(
    long DetailId,
    long ItemID,
    string ItemBarCode,
    int? Package,
    double Qty,
    double Price,
    double? DetailDiscount1,
    double? DetailDiscount2,
    double? DetailDiscount1Ratio,
    double? DetailTax,
    double? DetailTaxRatio,
    double? Value);
