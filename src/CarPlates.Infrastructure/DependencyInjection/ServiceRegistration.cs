using CarPlates.Application.Common.Interfaces;
using CarPlates.Infrastructure.Api;
using CarPlates.Infrastructure.Api.Authentication;
using CarPlates.Infrastructure.Camera;
using CarPlates.Infrastructure.Logging;
using CarPlates.Infrastructure.OCR;
using CarPlates.Infrastructure.Services;
using CarPlates.Infrastructure.Storage;
using CarPlates.Infrastructure.Storage.Database;
using CarPlates.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CarPlates.Infrastructure.DependencyInjection;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string dbPath,
        string apiUrl)
    {
        // Database
        services.AddSingleton<DatabaseContext>(_ => new DatabaseContext(dbPath));

        // Repositories
        services.AddScoped<IScanRepository, ScanRepository>();
        services.AddScoped<IPendingUploadRepository, PendingUploadRepository>();

        // Services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IVehicleLookupService, VehicleLookupService>();
        services.AddScoped<IPlateRecognitionService, PlateRecognitionService>();
        services.AddScoped<ICameraService, CameraService>();
        services.AddScoped<ISyncService, SyncService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<ITokenStorage, TokenStorage>();
        services.AddScoped<ILoggingService, LoggingService>();

        // HttpClient with Auth handler
        services.AddHttpClient("CarPlatesApi", client =>
        {
            client.BaseAddress = new Uri(apiUrl);
            client.Timeout = TimeSpan.FromSeconds(ApiConstants.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddHttpMessageHandler<AuthDelegatingHandler>();

        services.AddScoped<AuthDelegatingHandler>(sp =>
            new AuthDelegatingHandler(sp.GetRequiredService<ITokenStorage>(), apiUrl));

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
