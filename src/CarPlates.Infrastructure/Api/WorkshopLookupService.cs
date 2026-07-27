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
            result.Items.Select(t => new TechnicianResult(t.Id, t.Code, t.Name_Ar, t.Name_En)).ToList(),
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
            result.Items.Select(w => new WorkLocationResult(w.Id, w.Code, w.Name_Ar, w.Name_En)).ToList(),
            result.TotalCount, result.Page, result.PageSize, result.TotalPages);
    }

    public async Task<TechnicianResult> RegisterTechnicianAsync(CreateTechnicianRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsJsonAsync("technicians", request, ApiJsonOptions.Default, cancellationToken);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<TechnicianApiResponse>(ApiJsonOptions.Default, cancellationToken);
        return new TechnicianResult(dto!.Id, dto.Code, dto.Name_Ar, dto.Name_En);
    }

    public async Task<TechnicianResult> UpdateTechnicianAsync(int id, UpdateTechnicianRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Client.PutAsJsonAsync($"technicians/{id}", request, ApiJsonOptions.Default, cancellationToken);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<TechnicianApiResponse>(ApiJsonOptions.Default, cancellationToken);
        return new TechnicianResult(dto!.Id, dto.Code, dto.Name_Ar, dto.Name_En);
    }

    public async Task DeleteTechnicianAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await Client.DeleteAsync($"technicians/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<WorkLocationResult> RegisterWorkLocationAsync(CreateWorkLocationRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsJsonAsync("worklocations", request, ApiJsonOptions.Default, cancellationToken);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<WorkLocationApiResponse>(ApiJsonOptions.Default, cancellationToken);
        return new WorkLocationResult(dto!.Id, dto.Code, dto.Name_Ar, dto.Name_En);
    }

    public async Task<WorkLocationResult> UpdateWorkLocationAsync(int id, UpdateWorkLocationRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Client.PutAsJsonAsync($"worklocations/{id}", request, ApiJsonOptions.Default, cancellationToken);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<WorkLocationApiResponse>(ApiJsonOptions.Default, cancellationToken);
        return new WorkLocationResult(dto!.Id, dto.Code, dto.Name_Ar, dto.Name_En);
    }

    public async Task DeleteWorkLocationAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await Client.DeleteAsync($"worklocations/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private record TechnicianApiResponse(int Id, int? Code, string? Name_Ar, string? Name_En);
    private record WorkLocationApiResponse(int Id, int? Code, string? Name_Ar, string? Name_En);

    private record ApiPagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize, int TotalPages);
}
