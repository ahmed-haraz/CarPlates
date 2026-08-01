using CarPlates.API.Common;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CarModelsController(ICustomerCarService customerCarService, IUserContext userContext) : ControllerBase
{
    private readonly ICustomerCarService _customerCarService = customerCarService;

    [HttpGet]
    public async Task<ActionResult<PagedResult<CarModelDto>>> GetAll(
        [FromQuery] int? makeId = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _customerCarService.GetModelsPagedAsync(makeId, search, page, pageSize, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CarModelDto>> Create([FromBody] RegisterCarModelRequestDto request)
    {
        var userId = long.TryParse(userContext.UserId, out var uid) ? (long?)uid : null;
        var model = await _customerCarService.CreateModelAsync(request, userId);
        return StatusCode(201, model);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CarModelDto>> Update(int id, [FromBody] RegisterCarModelRequestDto request)
    {
        var userId = long.TryParse(userContext.UserId, out var uid) ? (long?)uid : null;
        var model = await _customerCarService.UpdateModelAsync(id, request, userId);
        return Ok(model);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var userId = long.TryParse(userContext.UserId, out var uid) ? (long?)uid : null;
        await _customerCarService.DeleteModelAsync(id, userId);
        return NoContent();
    }
}
