using CarPlates.API.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]

public class LovController(ILovService lovService) : ControllerBase
{
    private readonly ILovService _lovService = lovService;

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetLovItems(int id, [FromQuery] string? lang = "ar", [FromQuery] string? where = null)
    {
        var items = await _lovService.GetLovItemsAsync(id, lang, where);
        return Ok(items);
    }
}
