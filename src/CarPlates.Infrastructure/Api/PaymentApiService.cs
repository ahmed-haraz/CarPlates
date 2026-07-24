using CarPlates.Application.Common.Interfaces;
using System.Net.Http.Json;

namespace CarPlates.Infrastructure.Api;

public class PaymentApiService(IHttpClientFactory httpClientFactory) : IPaymentApiService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<PayBillApiResult> PayAsync(PayBillApiRequest request, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("CarPlatesApi");
        var body = new
        {
            headerId = request.HeaderId,
            payments = request.Payments.Select(p => new { payType = p.PayType, amount = p.Amount }).ToList(),
            notes = request.Notes
        };

        var response = await client.PostAsJsonAsync($"bills/{request.HeaderId}/payment", body, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<PayBillApiResult>(cancellationToken: cancellationToken);

        return result ?? new PayBillApiResult(false, "Failed to parse payment response", null, 0, 0);
    }

    public async Task<ReceiptApiResult?> GetReceiptAsync(long headerId, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("CarPlatesApi");
        var response = await client.GetAsync($"bills/{headerId}/payment/receipt", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<ReceiptApiResult>(cancellationToken: cancellationToken);
    }
}
