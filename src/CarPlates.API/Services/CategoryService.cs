using CarPlates.API.Common;
using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

public class CategoryService(ApplicationDbContext context) : ICategoryService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<PagedResult<CategoryDto>> GetAllAsync(
        string? search, int? branchId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Categories.AsNoTracking().Where(c => c.Status == 1);

        if (branchId.HasValue)
        {
            query = query.Where(c => c.BranchID == branchId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                (c.Name_AR != null && c.Name_AR.Contains(search)) ||
                (c.Name_En != null && c.Name_En.Contains(search)));
        }

        query = query.OrderBy(c => c.Name_En ?? c.Name_AR);

        var paged = await query.ToPagedResultAsync(page, pageSize, cancellationToken);
        var items = paged.Items.Select(MapToDto).ToList();

        return new PagedResult<CategoryDto>(items, paged.TotalCount, paged.Page, paged.PageSize, paged.TotalPages);
    }

    private static CategoryDto MapToDto(ItemSubGroupView c) => new(
        c.ID, c.Code, c.Name_AR, c.Name_En, c.Groupname, c.ParentID, c.BranchID, c.Image);
}
