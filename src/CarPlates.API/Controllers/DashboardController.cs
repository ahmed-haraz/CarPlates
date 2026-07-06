using CarPlates.API.Interface;
using CarPlates.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DashboardController(IScanRecordService scanService) : ControllerBase
{
    private readonly IScanRecordService _scanService = scanService;

    [HttpGet("statistics")]
    public async Task<ActionResult<DashboardStatisticsDto>> GetStatistics()
    {
        var stats = await _scanService.GetStatisticsAsync();
        return Ok(stats);
    }
}
