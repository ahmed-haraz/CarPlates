using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class WorkLocationsController(IWorkshopLookupService lookupService) : ControllerBase
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
}
