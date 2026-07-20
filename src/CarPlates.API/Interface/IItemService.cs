using CarPlates.API.Models;

namespace CarPlates.API.Interface;

public interface IItemService
{
    Task<ItemBarCodeDto?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<PagedResult<ItemBarCodeDto>> GetAllAsync(string? search, int? categoryId, int page, int pageSize, CancellationToken cancellationToken = default);
}
