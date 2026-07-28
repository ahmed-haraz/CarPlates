using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarPlates.API.Models;

[Table("PaymentGatewaySettings")]
public class PaymentGatewaySetting
{
    [Key]
    public int Id { get; set; }

    public bool IsEnabled { get; set; }

    [MaxLength(100)]
    public string GatewayName { get; set; } = "Default";

    [MaxLength(200)]
    public string MerchantId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ApiKey { get; set; } = string.Empty;

    [MaxLength(500)]
    public string EndpointUrl { get; set; } = string.Empty;

    public string AdditionalSettings { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
