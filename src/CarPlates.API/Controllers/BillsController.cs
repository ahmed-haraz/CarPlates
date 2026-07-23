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

    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<BillDto>>> Search(
        [FromQuery] string? search = null,
        [FromQuery] int? transDateFrom = null,
        [FromQuery] int? transDateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Ok(await _billService.SearchAsync(search, transDateFrom, transDateTo, page, pageSize, userId, cancellationToken));
    }

    [HttpGet("today-stats")]
    public async Task<IActionResult> GetTodayStats(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var (todayBills, todayTotal) = await _billService.GetTodayStatsAsync(userId, cancellationToken);
        return Ok(new { TodayBills = todayBills, TodayTotal = todayTotal });
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
