using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Shared.Constants;
using System.Net.Http.Json;
using System.Web;

namespace CarPlates.Infrastructure.Api;

public class CustomerLookupService(IHttpClientFactory httpClientFactory) : ICustomerLookupService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private HttpClient Client => _httpClientFactory.CreateClient("CarPlatesApi");

    public async Task<PaginatedResult<CustomerLookupResult>> SearchAsync(
        string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(search)) query["search"] = search;
        query["page"] = page.ToString();
        query["pageSize"] = pageSize.ToString();

        var result = await Client.GetFromJsonAsync<ApiPagedResult<CustomerApiResponse>>(
            $"customers?{query}", ApiJsonOptions.Default, cancellationToken);

        if (result == null) return new PaginatedResult<CustomerLookupResult>([], 0, page, pageSize, 0);

        return new PaginatedResult<CustomerLookupResult>(
            result.Items.Select(c => new CustomerLookupResult(c.Id, c.Name_Ar, c.Name_En, c.Mobile, c.Phone1)).ToList(),
            result.TotalCount, result.Page, result.PageSize, result.TotalPages);
    }

    private record CustomerApiResponse(int Id, string Code, string Name_Ar, string Name_En, string? Mobile, string? Phone1, string? Email, string? Address);

    // Shape returned by the server's PagedResult<T> wrapper (see CarPlates.API.Models.PagedResult).
    private record ApiPagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize, int TotalPages);
}
