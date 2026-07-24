using CarPlates.API.Common;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/bills/{headerId:long}/[controller]")]
[Authorize]
public class PaymentController(IPaymentService paymentService, IUserContext userContext) : ControllerBase
{
    private readonly IPaymentService _paymentService = paymentService;

    [HttpPost]
    public async Task<ActionResult<PayBillResponse>> Pay(
        long headerId,
        [FromBody] PayBillRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Payments == null || request.Payments.Count == 0)
            return BadRequest(new PayBillResponse(false, "At least one payment method is required", null, 0, 0));

        var result = await _paymentService.PayAsync(request with { HeaderId = headerId }, userContext.UserId, cancellationToken);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("receipt")]
    public async Task<ActionResult<ReceiptDto>> GetReceipt(long headerId, CancellationToken cancellationToken)
    {
        var receipt = await _paymentService.GetReceiptAsync(headerId, cancellationToken);
        if (receipt == null)
            return NotFound();

        return Ok(receipt);
    }
}
