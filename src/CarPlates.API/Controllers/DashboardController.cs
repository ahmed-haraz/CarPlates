using CarPlates.API.Models.DTOs;
using CarPlates.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IScanRecordService _scanService;

    public DashboardController(IScanRecordService scanService)
    {
        _scanService = scanService;
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<DashboardStatisticsDto>> GetStatistics()
    {
        var stats = await _scanService.GetStatisticsAsync();
        return Ok(stats);
    }
}
