using CarPlates.Application.Common.Interfaces;
using CarPlates.Infrastructure.Api;
using CarPlates.Infrastructure.Api.Authentication;
using CarPlates.Infrastructure.Camera;
using CarPlates.Infrastructure.Logging;
using CarPlates.Infrastructure.OCR;
using CarPlates.Infrastructure.Services;
using CarPlates.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CarPlates.Infrastructure.DependencyInjection;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string apiUrl)
    {
        // Repositories - backed entirely by the API now, no local database
        services.AddScoped<IScanRepository, ScanApiRepository>();

        // Services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IVehicleLookupService, VehicleLookupService>();
        services.AddScoped<ICustomerCarLookupService, CustomerCarLookupService>();
        services.AddScoped<IWorkshopLookupService, WorkshopLookupService>();
        services.AddScoped<ICustomerLookupService, CustomerLookupService>();
        services.AddScoped<IItemLookupService, ItemLookupService>();
        services.AddScoped<IPlateRecognitionService, PlateRecognitionService>();
        services.AddScoped<ICameraService, CameraService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<ITokenStorage, TokenStorage>();
        services.AddScoped<ILoggingService, LoggingService>();
        services.AddScoped<IApiConnectivityService, ApiConnectivityService>();

        // Live-updatable API base URL: seeded from the persisted preference, but
        // changing it later (Settings -> Save) updates this singleton immediately,
        // which the HttpClient factory below reads on every CreateClient call -
        // no app restart needed to point at a different API.
        services.AddSingleton<IApiUrlProvider>(_ => new ApiUrlProvider(apiUrl));

        // HttpClient with Auth handler
        services.AddHttpClient("CarPlatesApi", (sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<IApiUrlProvider>().CurrentApiUrl);
            client.Timeout = TimeSpan.FromSeconds(ApiConstants.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddHttpMessageHandler<AuthDelegatingHandler>();

        services.AddScoped<AuthDelegatingHandler>(sp =>
            new AuthDelegatingHandler(sp.GetRequiredService<ITokenStorage>(), sp.GetRequiredService<IApiUrlProvider>()));

        // Logging - use a simple path that works on all platforms
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            StorageConstants.LogsDirectory);

        Directory.CreateDirectory(logDir);

        var logPath = Path.Combine(logDir, "app-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: StorageConstants.MaxLogFiles)
            .CreateLogger();

        return services;
    }
}
