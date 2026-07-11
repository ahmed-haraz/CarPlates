using CarPlates.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CarPlates.Infrastructure.Api;

public class ApiConnectivityService(
    IHttpClientFactory httpClientFactory,
    IApiUrlProvider apiUrlProvider,
    ILogger<ApiConnectivityService> logger) : IApiConnectivityService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IApiUrlProvider _apiUrlProvider = apiUrlProvider;
    private readonly ILogger<ApiConnectivityService> _logger = logger;

    public async Task<ConnectivityResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // HealthController is deliberately unversioned/unauthenticated
            // ("/Health", not "/api/v1/Health"), so this is built against the
            // API's root authority rather than the "CarPlatesApi" HttpClient's
            // versioned BaseAddress.
            var configuredUrl = _apiUrlProvider.CurrentApiUrl;
            var authority = new Uri(configuredUrl).GetLeftPart(UriPartial.Authority);
            var healthUri = new Uri($"{authority}/Health");

            var client = _httpClientFactory.CreateClient("CarPlatesApi");
            using var response = await client.GetAsync(healthUri, cancellationToken);

            return response.IsSuccessStatusCode
                ? new ConnectivityResult(true, null)
                : new ConnectivityResult(false, $"Server responded with {(int)response.StatusCode} {response.ReasonPhrase}");
        }
        catch (TaskCanceledException)
        {
            return new ConnectivityResult(false, "Connection timed out");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API connectivity test failed");
            return new ConnectivityResult(false, ex.Message);
        }
    }
}
