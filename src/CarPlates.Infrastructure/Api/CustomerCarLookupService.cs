using CarPlates.Application.Common.Interfaces;
using CarPlates.Shared.Constants;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

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
                return new CustomerCarScanResult(false, request.PlateNumber, null, null, null, null, null, null, null, false, false, false, $"API error: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<ScanApiResponse>(ApiJsonOptions.Default, cancellationToken);
            _loggingService.LogApi("customercars/scan", true, stopwatch.ElapsedMilliseconds);

            if (result == null)
            {
                return new CustomerCarScanResult(false, request.PlateNumber, null, null, null, null, null, null, null, false, false, false, "Invalid response");
            }

            if (result.Car == null)
            {
                // Legitimate "not registered" outcome, not an error - scanning no longer
                // auto-registers a customer/car, so an unmatched plate is expected here.
                return new CustomerCarScanResult(false, request.PlateNumber, null, null, null, null, null, null, null, false, false, false, null);
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
                null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _loggingService.LogApi("customercars/scan", false, stopwatch.ElapsedMilliseconds);
            _logger.LogError(ex, "Customer car scan error for {PlateNumber}", request.PlateNumber);
            return new CustomerCarScanResult(false, request.PlateNumber, null, null, null, null, null, null, null, false, false, false, ex.Message);
        }
    }

    public async Task<IReadOnlyList<CarMakeResult>> GetMakesAsync(CancellationToken cancellationToken = default)
    {
        var makes = await Client.GetFromJsonAsync<List<MakeApiResponse>>("customercars/makes", ApiJsonOptions.Default, cancellationToken);
        return makes?.Select(m => new CarMakeResult(m.MakeID, m.MakeName)).ToList() ?? [];
    }

    public async Task<IReadOnlyList<CarModelResult>> GetModelsAsync(int makeId, CancellationToken cancellationToken = default)
    {
        var models = await Client.GetFromJsonAsync<List<ModelApiResponse>>($"customercars/models/{makeId}", ApiJsonOptions.Default, cancellationToken);
        return models?.Select(m => new CarModelResult(m.ModelID, m.MakeID, m.ModelName)).ToList() ?? [];
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

    private record ScanApiResponse(CarApiResponse? Car, bool WasNewCar, bool WasNewCustomer, bool WasNewBranchLink);

    private record CarApiResponse(
        long Id,
        string? PlateNumber,
        string? VIN,
        string? Color,
        int? VehicleYear,
        string? MakeName,
        string? ModelName,
        string? CustomerName_Ar,
        string? CustomerName_En,
        string? CustomerMobile);

    private record MakeApiResponse(int MakeID, string MakeName);
    private record ModelApiResponse(int ModelID, int MakeID, string ModelName);
    private record VehicleTypeApiResponse(int Id, int? Code, string? Name_Ar, string? Name_En);
    private record EngineTypeApiResponse(int Id, int? Code, string? Name_Ar, string? Name_En);
}
