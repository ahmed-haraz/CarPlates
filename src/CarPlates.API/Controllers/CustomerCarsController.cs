using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CustomerCarsController(ICustomerCarService customerCarService, ILogger<CustomerCarsController> logger) : ControllerBase
{
    private readonly ICustomerCarService _customerCarService = customerCarService;
    private readonly ILogger<CustomerCarsController> _logger = logger;

    /// <summary>Plain lookup — does not register anything if the plate isn't found.</summary>
    [HttpGet("{plateNumber}")]
    public async Task<ActionResult<CustomerCarLookupDto>> GetByPlateNumber(string plateNumber)
    {
        var car = await _customerCarService.GetByPlateAsync(plateNumber);
        if (car == null)
        {
            return NotFound(new { Message = $"No customer car registered for plate {plateNumber}" });
        }

        return Ok(car);
    }

    /// <summary>
    /// Main mobile entry point: scan a plate. Returns the existing car+customer if the plate
    /// is already registered. If it isn't, Car comes back null and nothing is written to
    /// wh_Customers/wh_CustomersBranch/wh_CustomerCars - the scan itself is recorded
    /// separately, in wh_ScanRecords only, by the caller.
    /// </summary>
    [HttpPost("scan")]
    public async Task<ActionResult<CustomerCarScanResultDto>> Scan([FromBody] CustomerCarScanDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var result = await _customerCarService.ScanAsync(dto, userId);
        return Ok(result);
    }

    [HttpGet("makes")]
    public async Task<ActionResult<IReadOnlyList<CarMakeDto>>> GetMakes()
    {
        return Ok(await _customerCarService.GetMakesAsync());
    }

    [HttpGet("models/{makeId:int}")]
    public async Task<ActionResult<IReadOnlyList<CarModelDto>>> GetModels(int makeId)
    {
        return Ok(await _customerCarService.GetModelsAsync(makeId));
    }

    [HttpGet("vehicletypes")]
    public async Task<ActionResult<IReadOnlyList<VehicleTypeDto>>> GetVehicleTypes()
    {
        return Ok(await _customerCarService.GetVehicleTypesAsync());
    }

    [HttpGet("enginetypes")]
    public async Task<ActionResult<IReadOnlyList<EngineTypeDto>>> GetEngineTypes()
    {
        return Ok(await _customerCarService.GetEngineTypesAsync());
    }
}
