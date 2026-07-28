using CarPlates.Shared.Models;

namespace CarPlates.Application.Common.Interfaces;

public interface IPaymentGatewaySettingsApiService
{
    Task<PaymentGatewayConfig> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(PaymentGatewayConfig config, CancellationToken cancellationToken = default);
}
