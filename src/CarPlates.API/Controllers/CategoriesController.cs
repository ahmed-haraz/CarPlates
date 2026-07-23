using CarPlates.API.Common;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CategoriesController(ICategoryService categoryService, IUserContext userContext) : ControllerBase
{
    private readonly ICategoryService _categoryService = categoryService;

    [HttpGet]
    public async Task<ActionResult<PagedResult<CategoryDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] int? branchId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var effectiveBranchId = branchId ?? userContext.BranchId;
        return Ok(await _categoryService.GetAllAsync(search, effectiveBranchId, page, pageSize, cancellationToken));
    }
}
