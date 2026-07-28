namespace CarPlates.Shared.Models;

public class PaymentGatewayConfig
{
    public bool IsEnabled { get; set; }
    public string GatewayName { get; set; } = "Default";
    public string MerchantId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public string AdditionalSettings { get; set; } = string.Empty;
}
