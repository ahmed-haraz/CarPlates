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
using System.Net.Http;
using System.Net.Security;

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
        services.AddScoped<IBillApiService, BillApiService>();
        services.AddScoped<IBillAttachmentApiService, BillAttachmentApiService>();
        services.AddScoped<IPaymentApiService, PaymentApiService>();
        services.AddScoped<IPaymentGatewaySettingsApiService, PaymentGatewaySettingsApiService>();
        services.AddScoped<IVehicleColorApiService, VehicleColorApiService>();
        services.AddScoped<IPlateRecognitionService, PlateRecognitionService>();
        services.AddScoped<ICameraService, CameraService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<ICompanyApiService, CompanyApiService>();
        services.AddScoped<ITokenStorage, TokenStorage>();
        services.AddScoped<ILoggingService, LoggingService>();
        services.AddScoped<IApiConnectivityService, ApiConnectivityService>();
        services.AddSingleton<IReceiptTemplateService, ReceiptTemplateService>();

        // Live-updatable API base URL: seeded from the persisted preference, but
        // changing it later (Settings -> Save) updates this singleton immediately,
        // which the HttpClient factory below reads on every CreateClient call -
        // no app restart needed to point at a different API.
        services.AddSingleton<IApiUrlProvider>(_ => new ApiUrlProvider(apiUrl));

        // HttpClient with Auth handler + company code header.
        // The API runs on an insecure/self-signed HTTPS certificate, so the
        // primary handler is configured to skip certificate validation - that
        // is what makes Android report net_http_ssl_connection_failed otherwise.
        services.AddHttpClient("CarPlatesApi", (sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<IApiUrlProvider>().CurrentApiUrl);
            client.Timeout = TimeSpan.FromSeconds(ApiConstants.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .ConfigurePrimaryHttpMessageHandler(CreateInsecureHttpMessageHandler)
        .AddHttpMessageHandler<AuthDelegatingHandler>()
        .AddHttpMessageHandler<CompanyCodeDelegatingHandler>();

        services.AddScoped<AuthDelegatingHandler>(sp =>
            new AuthDelegatingHandler(sp.GetRequiredService<ITokenStorage>(), sp.GetRequiredService<IApiUrlProvider>()));

        services.AddTransient<CompanyCodeDelegatingHandler>();

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

    // Accepts ANY server certificate. The API runs on a self-signed/invalid HTTPS
    // cert, and without this the native handler on Android fails with
    // net_http_ssl_connection_failed. Development workaround only.
    public static HttpMessageHandler CreateInsecureHttpMessageHandler()
    {
        return new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (message, cert, chain, errors) => true
            }
        };
    }
}
