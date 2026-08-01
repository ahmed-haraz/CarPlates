using CarPlates.API.Common;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TechniciansController(IWorkshopLookupService lookupService, IUserContext userContext) : ControllerBase
{
    private readonly IWorkshopLookupService _lookupService = lookupService;

    [HttpGet]
    public async Task<ActionResult<PagedResult<TechnicianDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _lookupService.GetTechniciansAsync(search, page, pageSize, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<TechnicianDto>> Register([FromBody] RegisterTechnicianRequestDto request)
    {
        var userId = long.TryParse(userContext.UserId, out var uid) ? (long?)uid : null;
        var technician = await _lookupService.RegisterTechnicianAsync(request, userId);
        return CreatedAtAction(nameof(GetAll), new { id = technician.Id }, technician);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TechnicianDto>> Update(int id, [FromBody] RegisterTechnicianRequestDto request)
    {
        var userId = long.TryParse(userContext.UserId, out var uid) ? (long?)uid : null;
        var technician = await _lookupService.UpdateTechnicianAsync(id, request, userId);
        return Ok(technician);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var userId = long.TryParse(userContext.UserId, out var uid) ? (long?)uid : null;
        await _lookupService.DeleteTechnicianAsync(id, userId);
        return NoContent();
    }
}
