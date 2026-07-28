using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Shared.Constants;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Web;

namespace CarPlates.Infrastructure.Api;

public class CustomerCarLookupService(
    IHttpClientFactory httpClientFactory,
    ILoggingService loggingService,
    ILogger<CustomerCarLookupService> logger) : ICustomerCarLookupService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private HttpClient Client => _httpClientFactory.CreateClient("CarPlatesApi");
    private readonly ILoggingService _loggingService = loggingService;
    private readonly ILogger<CustomerCarLookupService> _logger = logger;

    public async Task<CustomerCarScanResult> ScanAsync(CustomerCarScanRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Scanning customer car: {PlateNumber}", request.PlateNumber);

            var response = await Client.PostAsJsonAsync("customercars/scan", request, ApiJsonOptions.Default, cancellationToken);
            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                _loggingService.LogApi("customercars/scan", false, stopwatch.ElapsedMilliseconds);
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                return new CustomerCarScanResult(false, request.PlateNumber, null, null, null, null, null, null, null, false, false, false, null, $"API error: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<ScanApiResponse>(ApiJsonOptions.Default, cancellationToken);
            _loggingService.LogApi("customercars/scan", true, stopwatch.ElapsedMilliseconds);

            if (result == null)
            {
                return new CustomerCarScanResult(false, request.PlateNumber, null, null, null, null, null, null, null, false, false, false, null, "Invalid response");
            }

            if (result.Car == null)
            {
                return new CustomerCarScanResult(false, request.PlateNumber, null, null, null, null, null, null, null, false, false, false, null, null);
            }

            var car = result.Car;
            return new CustomerCarScanResult(
                true,
                car.PlateNumber,
                car.MakeName,
                car.ModelName,
                car.Color,
                car.VehicleYear,
                car.CustomerName_Ar,
                car.CustomerName_En,
                car.CustomerMobile,
                result.WasNewCar,
                result.WasNewCustomer,
                result.WasNewBranchLink,
                car.Id,
                null,
                car.MakeName_Ar,
                car.MakeName_En,
                car.ModelName_Ar,
                car.ModelName_En);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _loggingService.LogApi("customercars/scan", false, stopwatch.ElapsedMilliseconds);
            _logger.LogError(ex, "Customer car scan error for {PlateNumber}", request.PlateNumber);
            return new CustomerCarScanResult(false, request.PlateNumber, null, null, null, null, null, null, null, false, false, false, null, ex.Message);
        }
    }

    public async Task<IReadOnlyList<CarMakeResult>> GetMakesAsync(CancellationToken cancellationToken = default)
    {
        var makes = await Client.GetFromJsonAsync<List<MakeApiResponse>>("customercars/makes", ApiJsonOptions.Default, cancellationToken);
        return makes?.Select(m => new CarMakeResult(m.MakeID, m.Code, m.Name_ar, m.Name_en, m.IconOriginalURL)).ToList() ?? [];
    }

    public async Task<IReadOnlyList<CarModelResult>> GetModelsAsync(int makeId, CancellationToken cancellationToken = default)
    {
        var models = await Client.GetFromJsonAsync<List<ModelApiResponse>>($"customercars/models/{makeId}", ApiJsonOptions.Default, cancellationToken);
        return models?.Select(m => new CarModelResult(m.ModelID, m.MakeID, m.Code, m.Name_ar, m.Name_en)).ToList() ?? [];
    }

    public async Task<IReadOnlyList<VehicleTypeResult>> GetVehicleTypesAsync(CancellationToken cancellationToken = default)
    {
        var types = await Client.GetFromJsonAsync<List<VehicleTypeApiResponse>>("customercars/vehicletypes", ApiJsonOptions.Default, cancellationToken);
        return types?.Select(v => new VehicleTypeResult(v.Id, v.Name_Ar, v.Name_En)).ToList() ?? [];
    }

    public async Task<IReadOnlyList<EngineTypeResult>> GetEngineTypesAsync(CancellationToken cancellationToken = default)
    {
        var types = await Client.GetFromJsonAsync<List<EngineTypeApiResponse>>("customercars/enginetypes", ApiJsonOptions.Default, cancellationToken);
        return types?.Select(v => new EngineTypeResult(v.Id, v.Name_Ar, v.Name_En)).ToList() ?? [];
    }

    public async Task<PaginatedResult<CarMakeResult>> GetMakesPagedAsync(string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(search)) query["search"] = search;
        query["page"] = page.ToString();
        query["pageSize"] = pageSize.ToString();

        var result = await Client.GetFromJsonAsync<ApiPagedResult<MakeApiResponse>>(
            $"CarMakes?{query}", ApiJsonOptions.Default, cancellationToken);

        if (result == null) return new PaginatedResult<CarMakeResult>([], 0, page, pageSize, 0);

        return new PaginatedResult<CarMakeResult>(
            result.Items.Select(m => new CarMakeResult(m.MakeID, m.Code, m.Name_ar, m.Name_en, m.IconOriginalURL)).ToList(),
            result.TotalCount, result.Page, result.PageSize, result.TotalPages);
    }

    public async Task<PaginatedResult<CarModelResult>> GetModelsPagedAsync(int? makeId = null, string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (makeId.HasValue) query["makeId"] = makeId.Value.ToString();
        if (!string.IsNullOrWhiteSpace(search)) query["search"] = search;
        query["page"] = page.ToString();
        query["pageSize"] = pageSize.ToString();

        var result = await Client.GetFromJsonAsync<ApiPagedResult<ModelApiResponse>>(
            $"CarModels?{query}", ApiJsonOptions.Default, cancellationToken);

        if (result == null) return new PaginatedResult<CarModelResult>([], 0, page, pageSize, 0);

        return new PaginatedResult<CarModelResult>(
            result.Items.Select(m => new CarModelResult(m.ModelID, m.MakeID, m.Code, m.Name_ar, m.Name_en)).ToList(),
            result.TotalCount, result.Page, result.PageSize, result.TotalPages);
    }

    public async Task<CarMakeResult> CreateMakeAsync(CreateMakeRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsJsonAsync("CarMakes", request, ApiJsonOptions.Default, cancellationToken);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<MakeApiResponse>(ApiJsonOptions.Default, cancellationToken);
        return new CarMakeResult(dto!.MakeID, dto.Code, dto.Name_ar, dto.Name_en, dto.IconOriginalURL);
    }

    public async Task<CarMakeResult> UpdateMakeAsync(int id, UpdateMakeRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Client.PutAsJsonAsync($"CarMakes/{id}", request, ApiJsonOptions.Default, cancellationToken);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<MakeApiResponse>(ApiJsonOptions.Default, cancellationToken);
        return new CarMakeResult(dto!.MakeID, dto.Code, dto.Name_ar, dto.Name_en, dto.IconOriginalURL);
    }

    public async Task DeleteMakeAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await Client.DeleteAsync($"CarMakes/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<CarModelResult> CreateModelAsync(CreateModelRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsJsonAsync("CarModels", request, ApiJsonOptions.Default, cancellationToken);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<ModelApiResponse>(ApiJsonOptions.Default, cancellationToken);
        return new CarModelResult(dto!.ModelID, dto.MakeID, dto.Code, dto.Name_ar, dto.Name_en);
    }

    public async Task<CarModelResult> UpdateModelAsync(int id, UpdateModelRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Client.PutAsJsonAsync($"CarModels/{id}", request, ApiJsonOptions.Default, cancellationToken);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<ModelApiResponse>(ApiJsonOptions.Default, cancellationToken);
        return new CarModelResult(dto!.ModelID, dto.MakeID, dto.Code, dto.Name_ar, dto.Name_en);
    }

    public async Task DeleteModelAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await Client.DeleteAsync($"CarModels/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private record ScanApiResponse(CarApiResponse? Car, bool WasNewCar, bool WasNewCustomer, bool WasNewBranchLink);

    private record CarApiResponse(
        long Id,
        string? PlateNumber,
        string? VIN,
        string? Color,
        int? VehicleYear,
        string? MakeName_Ar,
        string? MakeName_En,
        string? MakeName,
        string? ModelName_Ar,
        string? ModelName_En,
        string? ModelName,
        string? CustomerName_Ar,
        string? CustomerName_En,
        string? CustomerMobile);

    private record MakeApiResponse(int MakeID, int Code, string Name_ar, string Name_en, string? IconOriginalURL);
    private record ModelApiResponse(int ModelID, int MakeID, int Code, string Name_ar, string Name_en);
    private record VehicleTypeApiResponse(int Id, int? Code, string? Name_Ar, string? Name_En);
    private record EngineTypeApiResponse(int Id, int? Code, string? Name_Ar, string? Name_En);

    private record ApiPagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize, int TotalPages);
}
