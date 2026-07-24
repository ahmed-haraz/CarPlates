using CarPlates.Application.Common.Interfaces;
using CarPlates.Shared.Constants;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace CarPlates.Mobile.Services;

public interface IApiUrlMonitorService
{
    Task StartAsync();
    Task StopAsync();
}

public class ApiUrlMonitorService(
    IApiUrlProvider apiUrlProvider,
    ISettingsService settingsService,
    ILogger<ApiUrlMonitorService> logger) : IApiUrlMonitorService
{
    private readonly IApiUrlProvider _apiUrlProvider = apiUrlProvider;
    private readonly ISettingsService _settingsService = settingsService;
    private readonly ILogger<ApiUrlMonitorService> _logger = logger;
    private HubConnection? _connection;

    public async Task StartAsync()
    {
        try
        {
            var baseUrl = _apiUrlProvider.CurrentApiUrl.TrimEnd('/');
            var hubUrl = baseUrl.Replace("/api/v1/", "").TrimEnd('/') + SignalRConstants.HubPath;

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            _connection.On<IPUpdatePayload>(SignalRConstants.IPUpdateMethod, async (payload) =>
            {
                _logger.LogInformation("Received IP update: {NewIP}", payload.NewIP);

                var newUrl = payload.NewIP.TrimEnd('/') + "/api/v1/";
                _apiUrlProvider.SetApiUrl(newUrl);
                await _settingsService.SetApiUrlAsync(newUrl);
            });

            await _connection.StartAsync();
            _logger.LogInformation("Connected to SignalR hub at {HubUrl}", hubUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to SignalR hub");
        }
    }

    public async Task StopAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private record IPUpdatePayload(string Id, string CompanyName, string NewIP, long Timestamp);
}
