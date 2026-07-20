using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ItemsController(IItemService itemService) : ControllerBase
{
    private readonly IItemService _itemService = itemService;

    /// <summary>Look an item up by its barcode (a single package/barcode row from vw_wh_ItemBarCodes).</summary>
    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<ItemBarCodeDto>> GetByBarcode(string barcode, CancellationToken cancellationToken)
    {
        var item = await _itemService.GetByBarcodeAsync(barcode, cancellationToken);
        if (item == null)
        {
            return NotFound(new { Message = $"No item found for barcode {barcode}" });
        }

        return Ok(item);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ItemBarCodeDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _itemService.GetAllAsync(search, categoryId, page, pageSize, cancellationToken));
    }

    /// <summary>All items belonging to one category (ItemSubGroupId), paginated.</summary>
    [HttpGet("category/{categoryId:int}")]
    public async Task<ActionResult<PagedResult<ItemBarCodeDto>>> GetByCategory(
        int categoryId,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _itemService.GetAllAsync(search, categoryId, page, pageSize, cancellationToken));
    }
}
