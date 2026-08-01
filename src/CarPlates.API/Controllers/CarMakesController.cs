using CarPlates.API.Common;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CarMakesController(ICustomerCarService customerCarService, IUserContext userContext) : ControllerBase
{
    private readonly ICustomerCarService _customerCarService = customerCarService;

    [HttpGet]
    public async Task<ActionResult<PagedResult<CarMakeDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _customerCarService.GetMakesPagedAsync(search, page, pageSize, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CarMakeDto>> Create([FromBody] RegisterCarMakeRequestDto request)
    {
        var userId = long.TryParse(userContext.UserId, out var uid) ? (long?)uid : null;
        var make = await _customerCarService.CreateMakeAsync(request, userId);
        return StatusCode(201, make);
    }


    [HttpPut("{id:int}")]
    public async Task<ActionResult<CarMakeDto>> Update(int id, [FromBody] RegisterCarMakeRequestDto request)
    {
        var userId = long.TryParse(userContext.UserId, out var uid) ? (long?)uid : null;
        var make = await _customerCarService.UpdateMakeAsync(id, request, userId);
        return Ok(make);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var userId = long.TryParse(userContext.UserId, out var uid) ? (long?)uid : null;
        await _customerCarService.DeleteMakeAsync(id, userId);
        return NoContent();
    }
}
