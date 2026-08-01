using CarPlates.API.Common;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class WorkLocationsController(IWorkshopLookupService lookupService, IUserContext userContext) : ControllerBase
{
    private readonly IWorkshopLookupService _lookupService = lookupService;

    [HttpGet]
    public async Task<ActionResult<PagedResult<WorkLocationDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _lookupService.GetWorkLocationsAsync(search, page, pageSize, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<WorkLocationDto>> Register([FromBody] RegisterWorkLocationRequestDto request)
    {
        var userId = long.TryParse(userContext.UserId, out var uid) ? (long?)uid : null;
        var workLocation = await _lookupService.RegisterWorkLocationAsync(request, userId);
        return CreatedAtAction(nameof(GetAll), new { id = workLocation.Id }, workLocation);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<WorkLocationDto>> Update(int id, [FromBody] RegisterWorkLocationRequestDto request)
    {
        var userId = long.TryParse(userContext.UserId, out var uid) ? (long?)uid : null;
        var workLocation = await _lookupService.UpdateWorkLocationAsync(id, request, userId);
        return Ok(workLocation);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var userId = long.TryParse(userContext.UserId, out var uid) ? (long?)uid : null;
        await _lookupService.DeleteWorkLocationAsync(id, userId);
        return NoContent();
    }
}
