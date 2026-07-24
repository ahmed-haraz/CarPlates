using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Shared.Constants;
using System.Net.Http.Json;
using System.Web;

namespace CarPlates.Infrastructure.Api;

public class ItemLookupService(IHttpClientFactory httpClientFactory) : IItemLookupService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private HttpClient Client => _httpClientFactory.CreateClient("CarPlatesApi");

    public async Task<ItemLookupResult?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var response = await Client.GetAsync($"items/barcode/{barcode}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();

        var item = await response.Content.ReadFromJsonAsync<ItemApiResponse>(ApiJsonOptions.Default, cancellationToken);
        return item == null ? null : ToResult(item);
    }

    public async Task<PaginatedResult<ItemLookupResult>> SearchAsync(
        string? search = null, int? categoryId = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(search)) query["search"] = search;
        if (categoryId.HasValue) query["categoryId"] = categoryId.Value.ToString();
        query["page"] = page.ToString();
        query["pageSize"] = pageSize.ToString();

        var result = await Client.GetFromJsonAsync<ApiPagedResult<ItemApiResponse>>(
            $"items?{query}", ApiJsonOptions.Default, cancellationToken);

        if (result == null) return new PaginatedResult<ItemLookupResult>([], 0, page, pageSize, 0);

        return new PaginatedResult<ItemLookupResult>(
            result.Items.Select(ToResult).ToList(),
            result.TotalCount, result.Page, result.PageSize, result.TotalPages);
    }

    public async Task<PaginatedResult<CategoryLookupResult>> GetCategoriesAsync(
        string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(search)) query["search"] = search;
        query["page"] = page.ToString();
        query["pageSize"] = pageSize.ToString();

        var result = await Client.GetFromJsonAsync<ApiPagedResult<CategoryApiResponse>>(
            $"categories?{query}", ApiJsonOptions.Default, cancellationToken);

        if (result == null) return new PaginatedResult<CategoryLookupResult>([], 0, page, pageSize, 0);

        return new PaginatedResult<CategoryLookupResult>(
            result.Items.Select(c => new CategoryLookupResult(c.Id, c.Name_Ar, c.Name_En, c.GroupName)).ToList(),
            result.TotalCount, result.Page, result.PageSize, result.TotalPages);
    }

    private static ItemLookupResult ToResult(ItemApiResponse i) => new(
        i.Id, i.Name_Ar, i.Name_En, i.ItemBarCode, i.Package, i.PackagePrice, i.ItemGroupId,
        i.ItemGroupName_Ar, i.ItemGroupName_En, i.ItemTax,
        i.OpenSale, ParseDiscount(i.ItemDiscount1), ParseDiscount(i.ItemDiscount2), ParseDiscount(i.ItemDiscount3));

    private static double? ParseDiscount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var cleaned = value.Trim().TrimEnd('%');
        if (double.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;
        return null;
    }

    private record ItemApiResponse(
        long Id, int? Code, string? Name_Ar, string? Name_En, string ItemBarCode,
        int? Package, string? PackageName, double? PackagePrice,
        int? ItemGroupId, string? ItemGroupName_Ar, string? ItemGroupName_En,
        double? ItemTax, byte? Status,
        bool OpenSale = false, string? ItemDiscount1 = null, string? ItemDiscount2 = null, string? ItemDiscount3 = null);

    private record CategoryApiResponse(int Id, int? Code, string? Name_Ar, string? Name_En, string? GroupName, int? ParentID, int? BranchID, string? Image);

    // Shape returned by the server's PagedResult<T> wrapper (see CarPlates.API.Models.PagedResult).
    private record ApiPagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize, int TotalPages);
}
