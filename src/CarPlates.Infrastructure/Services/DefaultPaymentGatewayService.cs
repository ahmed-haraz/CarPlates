using CarPlates.Application.Common.Interfaces;
using CarPlates.Shared.Models;

namespace CarPlates.Infrastructure.Services;

public class DefaultPaymentGatewayService : IPaymentGatewayService
{
    public PaymentGatewayConfig Config { get; set; } = new();
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Config.MerchantId) &&
        !string.IsNullOrWhiteSpace(Config.ApiKey) &&
        Config.IsEnabled;

    public Task<PaymentGatewayResult> ProcessAsync(PaymentGatewayRequest request)
    {
        // Default implementation — logs the request for future gateway integration.
        // Replace this with a real payment gateway SDK (Stripe, PayPal, Mada, etc.)
        System.Diagnostics.Debug.WriteLine(
            $"[PaymentGateway] Would process {request.Amount} {request.Currency} " +
            $"via {Config.GatewayName}. Card: {request.Card?.CardNumber?[..4]}****");

        return Task.FromResult(new PaymentGatewayResult
        {
            Success = true,
            TransactionId = $"TXN-{Guid.NewGuid():N}"[..20],
            Message = "Payment processed (mock). Replace DefaultPaymentGatewayService with a real gateway."
        });
    }
}
