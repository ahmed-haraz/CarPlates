using CarPlates.Application.Common.Interfaces;
using CarPlates.Shared.Models;
using System.Net.Http.Json;

namespace CarPlates.Infrastructure.Api;

public class PaymentGatewaySettingsApiService(IHttpClientFactory httpClientFactory) : IPaymentGatewaySettingsApiService
{
    public async Task<PaymentGatewayConfig> GetAsync(CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("CarPlatesApi");
        var response = await client.GetAsync("payment-gateway-settings", cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new PaymentGatewayConfig();

        var result = await response.Content.ReadFromJsonAsync<PaymentGatewayConfig>(cancellationToken: cancellationToken);
        return result ?? new PaymentGatewayConfig();
    }

    public async Task SaveAsync(PaymentGatewayConfig config, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("CarPlatesApi");
        var response = await client.PostAsJsonAsync("payment-gateway-settings", config, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
