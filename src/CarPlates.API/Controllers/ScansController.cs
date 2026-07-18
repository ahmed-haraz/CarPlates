using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ScansController(IScanRecordService scanService) : ControllerBase
{
    private readonly IScanRecordService _scanService = scanService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ScanRecordDto>>> GetAll(
        [FromQuery] string? plateNumber = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var records = await _scanService.GetAllAsync(plateNumber, startDate, endDate);
        return Ok(records);
    }

    [HttpGet("recent")]
    public async Task<ActionResult<IReadOnlyList<RecentScanDto>>> GetRecent([FromQuery] int count = 10)
    {
        var records = await _scanService.GetRecentAsync(count);
        return Ok(records);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ScanRecordDto>> GetById(long id)
    {
        var record = await _scanService.GetByIdAsync(id);
        if (record == null) return NotFound();
        return Ok(record);
    }

    [HttpPost]
    public async Task<ActionResult<ScanRecordDto>> Create([FromBody] ScanRecordCreateDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var record = await _scanService.CreateAsync(dto, userId);
        return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
    }

    [HttpPost("sync")]
    public async Task<ActionResult<SyncBatchResponseDto>> SyncBatch([FromBody] SyncBatchRequestDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var result = await _scanService.SyncBatchAsync(dto, userId);
        return Ok(result);
    }
}
