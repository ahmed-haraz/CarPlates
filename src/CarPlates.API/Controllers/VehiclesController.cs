using CarPlates.API.Models.DTOs;
using CarPlates.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;
    private readonly ILogger<VehiclesController> _logger;

    public VehiclesController(IVehicleService vehicleService, ILogger<VehiclesController> logger)
    {
        _vehicleService = vehicleService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VehicleDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        var vehicles = await _vehicleService.GetAllAsync(search, status);
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

    [HttpGet("id/{id:guid}")]
    public async Task<ActionResult<VehicleDto>> GetById(Guid id)
    {
        var vehicle = await _vehicleService.GetByIdAsync(id);
        if (vehicle == null) return NotFound();
        return Ok(vehicle);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<ActionResult<VehicleDto>> Create([FromBody] VehicleCreateDto dto)
    {
        if (await _vehicleService.ExistsAsync(dto.PlateNumber))
        {
            return Conflict(new { Message = "Vehicle with this plate number already exists" });
        }

        var vehicle = await _vehicleService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, vehicle);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<ActionResult<VehicleDto>> Update(Guid id, [FromBody] VehicleUpdateDto dto)
    {
        var vehicle = await _vehicleService.UpdateAsync(id, dto);
        if (vehicle == null) return NotFound();
        return Ok(vehicle);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _vehicleService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}
