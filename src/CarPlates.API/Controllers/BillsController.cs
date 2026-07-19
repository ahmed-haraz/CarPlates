using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class BillsController(IBillService billService) : ControllerBase
{
    private readonly IBillService _billService = billService;

    [HttpGet]
    public async Task<ActionResult<PagedResult<BillDto>>> GetAll(
        [FromQuery] int? branchId = null,
        [FromQuery] int? customerId = null,
        [FromQuery] int? carHeaderId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _billService.GetAllAsync(branchId, customerId, carHeaderId, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<BillDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var bill = await _billService.GetByIdAsync(id, cancellationToken);
        if (bill == null) return NotFound();
        return Ok(bill);
    }

    [HttpPost]
    public async Task<ActionResult<BillDto>> Create([FromBody] CreateBillDto dto, CancellationToken cancellationToken)
    {
        if (dto.Details == null || dto.Details.Count == 0)
        {
            return BadRequest(new { Message = "A bill needs at least one detail line" });
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var bill = await _billService.CreateAsync(dto, userId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = bill.HeaderId }, bill);
    }
}
