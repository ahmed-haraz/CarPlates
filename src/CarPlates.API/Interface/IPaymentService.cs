using CarPlates.API.Models;

namespace CarPlates.API.Interface;

public interface IPaymentService
{
    Task<PayBillResponse> PayAsync(PayBillRequest request, string? userId, CancellationToken cancellationToken = default);
    Task<ReceiptDto?> GetReceiptAsync(long headerId, CancellationToken cancellationToken = default);
}
