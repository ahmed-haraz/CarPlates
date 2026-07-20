using CarPlates.API.Common;
using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

public class ItemService(ApplicationDbContext context) : IItemService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ItemBarCodeDto?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var trimmed = barcode.Trim();

        var item = await _context.ItemBarCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ItemBarCode == trimmed, cancellationToken);

        return item == null ? null : MapToDto(item);
    }

    public async Task<PagedResult<ItemBarCodeDto>> GetAllAsync(
        string? search, int? categoryId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.ItemBarCodes.AsNoTracking().AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(i => i.ItemSubGroupId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i =>
                i.ItemBarCode.Contains(search) ||
                (i.Name_Ar != null && i.Name_Ar.Contains(search)) ||
                (i.Name_En != null && i.Name_En.Contains(search)));
        }

        query = query.OrderBy(i => i.Name_En ?? i.Name_Ar);

        var paged = await query.ToPagedResultAsync(page, pageSize, cancellationToken);
        var items = paged.Items.Select(MapToDto).ToList();

        return new PagedResult<ItemBarCodeDto>(items, paged.TotalCount, paged.Page, paged.PageSize, paged.TotalPages);
    }

    private static ItemBarCodeDto MapToDto(ItemBarCodeView i) => new(
        i.ID,
        i.Code,
        i.Name_Ar,
        i.Name_En,
        i.ItemBarCode,
        i.Package,
        i.PackageName,
        i.PackagePrice,
        i.ItemGroupId,
        i.ItemGroupName_AR,
        i.ItemGroupName_En,
        i.ItemTax,
        i.Status);
}
