namespace CarPlates.Application.Common.Interfaces;

public class PaymentGatewayRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SAR";
    public CardInfo? Card { get; set; }
    public string? Description { get; set; }
    public string? TransactionRef { get; set; }
}

public class PaymentGatewayResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? AuthorizationCode { get; set; }
    public string? Message { get; set; }
    public string? RawResponse { get; set; }
}

public class PaymentGatewayConfig
{
    public bool IsEnabled { get; set; }
    public string GatewayName { get; set; } = "Default";
    public string MerchantId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public string AdditionalSettings { get; set; } = string.Empty;
}

public interface IPaymentGatewayService
{
    Task<PaymentGatewayResult> ProcessAsync(PaymentGatewayRequest request);
    PaymentGatewayConfig Config { get; set; }
    bool IsConfigured { get; }
}
