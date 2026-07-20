using CarPlates.Application.Common.DTOs;

namespace CarPlates.Application.Common.Interfaces;

public interface IItemLookupService
{
    Task<ItemLookupResult?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    Task<PaginatedResult<ItemLookupResult>> SearchAsync(
        string? search = null, int? categoryId = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    Task<PaginatedResult<CategoryLookupResult>> GetCategoriesAsync(
        string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}

public record ItemLookupResult(
    long Id,
    string? Name_Ar,
    string? Name_En,
    string ItemBarCode,
    double? PackagePrice,
    int? ItemGroupId,
    string? ItemGroupName_Ar,
    string? ItemGroupName_En,
    double? ItemTax);

public record CategoryLookupResult(int Id, string? Name_Ar, string? Name_En, string? GroupName);
