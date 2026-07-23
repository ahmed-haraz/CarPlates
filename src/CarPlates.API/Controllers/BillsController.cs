using CarPlates.API.Common;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class BillsController(IBillService billService, IUserContext userContext) : ControllerBase
{
    private readonly IBillService _billService = billService;

    [HttpGet]
    public async Task<ActionResult<PagedResult<BillDto>>> GetAll(
        [FromQuery] int? customerId = null,
        [FromQuery] int? carHeaderId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _billService.GetAllAsync(userContext.BranchId, customerId, carHeaderId, page, pageSize, cancellationToken));
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
        return Ok(await _billService.SearchAsync(search, transDateFrom, transDateTo, page, pageSize, userContext.UserId, userContext.BranchId, cancellationToken));
    }

    [HttpGet("today-stats")]
    public async Task<IActionResult> GetTodayStats(CancellationToken cancellationToken)
    {
        var (todayBills, todayTotal) = await _billService.GetTodayStatsAsync(userContext.UserId, userContext.BranchId, cancellationToken);
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

        var bill = await _billService.CreateAsync(dto, userContext.UserId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = bill.HeaderId }, bill);
    }
}
