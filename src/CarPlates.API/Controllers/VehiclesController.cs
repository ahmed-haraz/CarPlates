using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class VehiclesController(IVehicleService vehicleService, ILogger<VehiclesController> logger) : ControllerBase
{
    private readonly IVehicleService _vehicleService = vehicleService;
    private readonly ILogger<VehiclesController> _logger = logger;

    [HttpGet]
    public async Task<ActionResult<PagedResult<VehicleDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var vehicles = await _vehicleService.GetAllAsync(search, status, page, pageSize);
        return Ok(vehicles);
    }

    [HttpGet("{plateNumber}")]
    public async Task<ActionResult<VehicleDto>> GetByPlateNumber(string plateNumber)
    {
        var vehicle = await _vehicleService.GetByPlateNumberAsync(plateNumber.ToUpperInvariant());
        if (vehicle == null)
        {
            _logger.LogWarning("Vehicle not found: {PlateNumber}", plateNumber);
            return NotFound(new { Message = $"Vehicle with plate {plateNumber} not found" });
        }

        return Ok(vehicle);
    }

    [HttpGet("id/{id:int}")]
    public async Task<ActionResult<VehicleDto>> GetById(int id)
    {
        var vehicle = await _vehicleService.GetByIdAsync(id);
        if (vehicle == null) return NotFound();
        return Ok(vehicle);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<ActionResult<VehicleDto>> Create([FromBody] VehicleCreateDto dto)
    {
        if (dto.BranchID <= 0)
        {
            return BadRequest(new { Message = "BranchID is required" });
        }

        if (await _vehicleService.ExistsAsync(dto.PlateNumber))
        {
            return Conflict(new { Message = "Vehicle with this plate number already exists" });
        }

        var vehicle = await _vehicleService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, vehicle);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<ActionResult<VehicleDto>> Update(int id, [FromBody] VehicleUpdateDto dto)
    {
        var vehicle = await _vehicleService.UpdateAsync(id, dto);
        if (vehicle == null) return NotFound();
        return Ok(vehicle);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        var result = await _vehicleService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    
}
