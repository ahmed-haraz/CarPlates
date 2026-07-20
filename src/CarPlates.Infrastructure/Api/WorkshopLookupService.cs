using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Shared.Constants;
using System.Net.Http.Json;
using System.Web;

namespace CarPlates.Infrastructure.Api;

public class WorkshopLookupService(IHttpClientFactory httpClientFactory) : IWorkshopLookupService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private HttpClient Client => _httpClientFactory.CreateClient("CarPlatesApi");

    public async Task<PaginatedResult<TechnicianResult>> GetTechniciansAsync(
        string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(search)) query["search"] = search;
        query["page"] = page.ToString();
        query["pageSize"] = pageSize.ToString();

        var result = await Client.GetFromJsonAsync<ApiPagedResult<TechnicianApiResponse>>(
            $"technicians?{query}", ApiJsonOptions.Default, cancellationToken);

        if (result == null) return new PaginatedResult<TechnicianResult>([], 0, page, pageSize, 0);

        return new PaginatedResult<TechnicianResult>(
            result.Items.Select(t => new TechnicianResult(t.Id, t.Name_Ar, t.Name_En)).ToList(),
            result.TotalCount, result.Page, result.PageSize, result.TotalPages);
    }

    public async Task<PaginatedResult<WorkLocationResult>> GetWorkLocationsAsync(
        string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(search)) query["search"] = search;
        query["page"] = page.ToString();
        query["pageSize"] = pageSize.ToString();

        var result = await Client.GetFromJsonAsync<ApiPagedResult<WorkLocationApiResponse>>(
            $"worklocations?{query}", ApiJsonOptions.Default, cancellationToken);

        if (result == null) return new PaginatedResult<WorkLocationResult>([], 0, page, pageSize, 0);

        return new PaginatedResult<WorkLocationResult>(
            result.Items.Select(w => new WorkLocationResult(w.Id, w.Name_Ar, w.Name_En)).ToList(),
            result.TotalCount, result.Page, result.PageSize, result.TotalPages);
    }

    private record TechnicianApiResponse(int Id, int? Code, string? Name_Ar, string? Name_En);
    private record WorkLocationApiResponse(int Id, int? Code, string? Name_Ar, string? Name_En);

    // Shape returned by the server's PagedResult<T> wrapper (see CarPlates.API.Models.PagedResult).
    private record ApiPagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize, int TotalPages);
}
