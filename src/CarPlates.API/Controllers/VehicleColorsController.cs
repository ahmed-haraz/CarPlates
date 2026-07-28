using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class VehicleColorsController(IVehicleColorService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<VehicleColor>>> GetAll(CancellationToken cancellationToken)
    {
        var colors = await service.GetAllAsync(cancellationToken);
        return Ok(colors);
    }
}
