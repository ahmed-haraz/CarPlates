using CarPlates.Application.Common;
using CarPlates.Application.Common.Behaviors;
using CarPlates.Infrastructure.DependencyInjection;
using CarPlates.Mobile.Controls;
using CarPlates.Mobile.Navigation;
using CarPlates.Mobile.Theming;

using CarPlates.Mobile.ViewModels;
using CarPlates.Mobile.Views.About;
using CarPlates.Mobile.Views.Dashboard;
using CarPlates.Mobile.Views.History;
using CarPlates.Mobile.Views.Login;
using CarPlates.Mobile.Views.Main;
using CarPlates.Mobile.Views.Profile;
using CarPlates.Mobile.Views.Scanner;
using CarPlates.Mobile.Views.Actions;
using CarPlates.Mobile.Views.Settings;
using CarPlates.Mobile.Views.Splash;
using CarPlates.Mobile.Views.Vehicle;
using CarPlates.Shared.Constants;
using CommunityToolkit.Maui;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Reflection;

using CarPlates.Application.Common.Interfaces;
#if ANDROID
using CarPlates.Mobile.Platforms.Android.Handlers;
using CarPlates.Mobile.Platforms.Android.Services;
#elif IOS
using CarPlates.Mobile.Platforms.iOS;
using CarPlates.Mobile.Platforms.iOS.Services;
#endif

namespace CarPlates.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<CameraPreview, CameraPreviewHandler>();
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
                fonts.AddFont("icomoon.ttf", "IcoMoon");
            });

        //Preferences.Remove("api_url");

        var apiUrl = Preferences.Get("api_url", ApiConstants.DefaultApiUrl);

        // Infrastructure services
        builder.Services.AddInfrastructureServices(apiUrl);

        // MediatR
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.RegisterServicesFromAssembly(typeof(CarPlates.Application.Common.Interfaces.IAuthenticationService).Assembly);
        });

        // Pipeline behaviors
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        // AutoMapper
        builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));

        // Navigation (replaces Shell)
        builder.Services.AddSingleton<INavigationService, NavigationService>();

        // Theming
        builder.Services.AddSingleton<IThemeService, ThemeService>();

        // Tap-to-scan document scanner (platform-specific)
#if ANDROID || IOS
        builder.Services.AddSingleton<IDocumentScannerService, DocumentScannerService>();
#endif

        // ViewModels
        builder.Services.AddTransient<SplashViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ScannerViewModel>();
        builder.Services.AddTransient<HistoryViewModel>();
        builder.Services.AddTransient<VehicleDetailsViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<NewOrderViewModel>();
        builder.Services.AddTransient<CarDataViewModel>();
        builder.Services.AddTransient<CashierViewModel>();

        // Views
        builder.Services.AddTransient<SplashPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ScannerPage>();
        builder.Services.AddTransient<HistoryPage>();
        builder.Services.AddTransient<VehicleDetailsPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<AboutPage>();
        builder.Services.AddTransient<NewOrderPage>();
        builder.Services.AddTransient<CarDataPage>();
        builder.Services.AddTransient<CashierPage>();
        builder.Services.AddTransient<OrderSummaryPage>();
        builder.Services.AddTransient<BrandSelectionPage>();
        builder.Services.AddTransient<ModelSelectionPage>();
        builder.Services.AddTransient<MainTabbedPage>();

        // Logging
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
