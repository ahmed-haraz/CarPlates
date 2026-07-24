using CarPlates.API.Hubs;
using CarPlates.API.Interface;
using CarPlates.Shared.Constants;
using Microsoft.AspNetCore.SignalR;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarPlates.API.Services;

public class PublishIPMonitorService(
    IHubContext<ReceivedIP> hubContext,
    IHttpClientFactory httpClientFactory,
    ILogger<PublishIPMonitorService> logger,
    IDeviceValidationService deviceValidation) : BackgroundService
{
    private readonly IHubContext<ReceivedIP> _hubContext = hubContext;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<PublishIPMonitorService> _logger = logger;
    private readonly IDeviceValidationService _deviceValidation = deviceValidation;

    private HttpClient Client => _httpClientFactory.CreateClient("FwApi");

    private string? _lastKnownIP;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PublishIP monitor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckForIPChangeAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking published IP");
            }

            await Task.Delay(TimeSpan.FromSeconds(SignalRConstants.PollIntervalSeconds), stoppingToken);
        }
    }

    private async Task CheckForIPChangeAsync(CancellationToken ct)
    {
        var response = await Client.GetAsync("api/FwPublishIP/GetAll", ct);
        if (!response.IsSuccessStatusCode) return;

        var result = await response.Content.ReadFromJsonAsync<PublishIPResponse>(JsonOptions, ct);
        if (result?.PublishIPs == null) return;

        var tracked = result.PublishIPs.FirstOrDefault(p =>
            string.Equals(p.Id, SignalRConstants.DefaultPublishedIPId, StringComparison.OrdinalIgnoreCase));

        if (tracked == null) return;

        var currentIP = tracked.PublishIP?.TrimEnd('/');

        if (string.IsNullOrEmpty(currentIP)) return;

        if (_lastKnownIP != null && _lastKnownIP != currentIP)
        {
            _logger.LogInformation("Published IP changed: {Old} -> {New}", _lastKnownIP, currentIP);

            var message = new IPUpdateMessage
            {
                Id = tracked.Id,
                CompanyName = tracked.CompanyName ?? "",
                NewIP = currentIP,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await _hubContext.Clients.All.SendAsync(SignalRConstants.IPUpdateMethod, message, ct);

            _lastKnownIP = currentIP;
        }
        else if (_lastKnownIP == null)
        {
            _lastKnownIP = currentIP;
            _logger.LogInformation("Initial published IP: {IP}", currentIP);
        }
    }

    private record PublishIPResponse(
        [property: JsonPropertyName("isSuccess")] bool IsSuccess,
        [property: JsonPropertyName("publishIPs")] PublishIPEntry[]? PublishIPs);

    private record PublishIPEntry(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("companyName")] string? CompanyName,
        [property: JsonPropertyName("publishIP")] string? PublishIP,
        [property: JsonPropertyName("status")] int Status);
}
