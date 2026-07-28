using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/payment-gateway-settings")]
[Authorize]
public class PaymentGatewaySettingsController(IPaymentGatewaySettingsService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaymentGatewaySetting>> Get(CancellationToken cancellationToken)
    {
        var settings = await service.GetAsync(cancellationToken);
        if (settings == null)
            return Ok(new PaymentGatewaySetting());
        return Ok(settings);
    }

    [HttpPost]
    public async Task<ActionResult<PaymentGatewaySetting>> Save(
        [FromBody] PaymentGatewaySetting settings,
        CancellationToken cancellationToken)
    {
        var result = await service.SaveAsync(settings, cancellationToken);
        return Ok(result);
    }
}
