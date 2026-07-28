using CarPlates.Application.Common.Interfaces;
using CarPlates.Shared.Models;
using System.Net.Http.Json;

namespace CarPlates.Infrastructure.Api;

public class VehicleColorApiService(IHttpClientFactory httpClientFactory) : IVehicleColorApiService
{
    public async Task<List<VehicleColorDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("CarPlatesApi");
        var response = await client.GetAsync("vehicle-colors", cancellationToken);

        if (!response.IsSuccessStatusCode)
            return [];

        var colors = await response.Content.ReadFromJsonAsync<List<VehicleColorDto>>(cancellationToken: cancellationToken);
        return colors ?? [];
    }
}
