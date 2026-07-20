using CarPlates.API.Models;

namespace CarPlates.API.Interface;

public interface ICategoryService
{
    Task<PagedResult<CategoryDto>> GetAllAsync(string? search, int? branchId, int page, int pageSize, CancellationToken cancellationToken = default);
}
