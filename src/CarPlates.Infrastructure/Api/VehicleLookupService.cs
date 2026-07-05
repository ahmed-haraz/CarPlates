using System.Net.Http.Json;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CarPlates.Infrastructure.Api;

public class VehicleLookupService : IVehicleLookupService
{
    private readonly HttpClient _httpClient;
    private readonly ILoggingService _loggingService;
    private readonly ILogger<VehicleLookupService> _logger;

    public VehicleLookupService(
        IHttpClientFactory httpClientFactory,
        ILoggingService loggingService,
        ILogger<VehicleLookupService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("CarPlatesApi");
        _loggingService = loggingService;
        _logger = logger;
    }

    public async Task<VehicleLookupResult> LookupAsync(string plateNumber, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Looking up vehicle: {PlateNumber}", plateNumber);

            var response = await _httpClient.GetAsync($"vehicles/{plateNumber}", cancellationToken);
            stopwatch.Stop();

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _loggingService.LogApi($"vehicles/{plateNumber}", false, stopwatch.ElapsedMilliseconds);
                return new VehicleLookupResult(false, plateNumber, null, null, null, null, null, "Vehicle not found");
            }

            if (!response.IsSuccessStatusCode)
            {
                _loggingService.LogApi($"vehicles/{plateNumber}", false, stopwatch.ElapsedMilliseconds);
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                return new VehicleLookupResult(false, plateNumber, null, null, null, null, null, $"API error: {error}");
            }

            var vehicle = await response.Content.ReadFromJsonAsync<VehicleApiResponse>(cancellationToken);
            _loggingService.LogApi($"vehicles/{plateNumber}", true, stopwatch.ElapsedMilliseconds);

            if (vehicle == null)
            {
                return new VehicleLookupResult(false, plateNumber, null, null, null, null, null, "Invalid response");
            }

            return new VehicleLookupResult(
                true,
                vehicle.PlateNumber,
                vehicle.Brand,
                vehicle.Model,
                vehicle.Color,
                vehicle.OwnerName,
                vehicle.AccessStatus,
                null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _loggingService.LogApi($"vehicles/{plateNumber}", false, stopwatch.ElapsedMilliseconds);
            _logger.LogError(ex, "Vehicle lookup error for {PlateNumber}", plateNumber);
            return new VehicleLookupResult(false, plateNumber, null, null, null, null, null, ex.Message);
        }
    }

    private record VehicleApiResponse(
        string PlateNumber,
        string? Brand,
        string? Model,
        string? Color,
        string? OwnerName,
        string? AccessStatus);
}
