using CarPlates.Application.Common.Behaviors;
using CarPlates.Infrastructure.DependencyInjection;
using CarPlates.Mobile.ViewModels;
using CarPlates.Mobile.Views.Dashboard;
using CarPlates.Mobile.Views.History;
using CarPlates.Mobile.Views.Login;
using CarPlates.Mobile.Views.Profile;
using CarPlates.Mobile.Views.Scanner;
using CarPlates.Mobile.Views.Settings;
using CarPlates.Mobile.Views.Splash;
using CarPlates.Mobile.Views.Vehicle;
using CarPlates.Shared.Constants;
using CommunityToolkit.Maui;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CarPlates.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
            });

        // Database path
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, StorageConstants.DatabaseName);
        var apiUrl = Preferences.Get("api_url", ApiConstants.DefaultApiUrl);

        // Infrastructure services
        builder.Services.AddInfrastructureServices(dbPath, apiUrl);

        // MediatR
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.RegisterServicesFromAssembly(typeof(Application.Common.Interfaces.IAuthenticationService).Assembly);
        });

        // Pipeline behaviors
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        // AutoMapper
        builder.Services.AddAutoMapper(typeof(Application.Common.MappingProfile).Assembly);

        // ViewModels
        builder.Services.AddTransient<SplashViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ScannerViewModel>();
        builder.Services.AddTransient<HistoryViewModel>();
        builder.Services.AddTransient<VehicleDetailsViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();

        // Views
        builder.Services.AddTransient<SplashPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ScannerPage>();
        builder.Services.AddTransient<HistoryPage>();
        builder.Services.AddTransient<VehicleDetailsPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<ProfilePage>();

        // Logging
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
