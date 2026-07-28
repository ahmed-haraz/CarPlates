using CarPlates.API.Models;

namespace CarPlates.API.Interface;

public interface IPaymentGatewaySettingsService
{
    Task<PaymentGatewaySetting?> GetAsync(CancellationToken cancellationToken = default);
    Task<PaymentGatewaySetting> SaveAsync(PaymentGatewaySetting settings, CancellationToken cancellationToken = default);
}
