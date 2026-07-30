using CarPlates.Infrastructure.Api.Authentication;
using CarPlates.Mobile.Localization;

namespace CarPlates.Mobile;

public partial class App : Microsoft.Maui.Controls.Application
{
    private bool _sessionSubscribed;

    public App()
    {
        InitializeComponent();

        // Apply the persisted language choice (falls back to device locale via
        // CultureInfo.CurrentUICulture's default if none was ever saved) before any
        // page is created, so the very first frame is already in the right language/direction.
        var savedLanguage = Preferences.Get("language", "en");
        var culture = new System.Globalization.CultureInfo(savedLanguage == "ar" ? "ar" : "en");
        LocalizationResourceManager.Instance.SetCulture(culture);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Root page is resolved via DI (see MauiProgram) so the Splash page's
        // constructor-injected view model works the same way every other page's
        // does. Shell used to own this bootstrapping implicitly; now it's explicit.
        var services = IPlatformApplication.Current!.Services;

        var savedTheme = Preferences.Get("theme", "System");
        var theme = Enum.TryParse<CarPlates.Domain.Enums.AppTheme>(savedTheme, out var parsed)
            ? parsed
            : CarPlates.Domain.Enums.AppTheme.System;
        services.GetRequiredService<Theming.IThemeService>().ApplyTheme(theme);

        // Start SignalR monitor for live API URL updates
        _ = services.GetRequiredService<Services.IApiUrlMonitorService>().StartAsync();

        // Subscribe once to detect when another device logs in with the same credentials.
        SubscribeToSessionRevoked();

        var splashPage = services.GetRequiredService<Views.Splash.SplashPage>();
        NavigationPage.SetHasNavigationBar(splashPage, false);

        var rootNav = new NavigationPage(splashPage)
        {
            FlowDirection = LocalizationResourceManager.Instance.FlowDirection
        };
        return new Window(rootNav);
    }

    private void SubscribeToSessionRevoked()
    {
        if (_sessionSubscribed) return;
        _sessionSubscribed = true;

        AuthDelegatingHandler.SessionRevoked += OnSessionRevokedAsync;
    }

    private async Task OnSessionRevokedAsync()
    {
        try
        {
            // Must run on the UI thread to show dialogs and navigate.
            if (MainThread.IsMainThread)
                await ForceLogoutAsync();
            else
                await MainThread.InvokeOnMainThreadAsync(ForceLogoutAsync);
        }
        catch
        {
            // Best-effort — if the UI is already gone, nothing to do.
        }
    }

    private async Task ForceLogoutAsync()
    {
        try
        {
            var navService = IPlatformApplication.Current?.Services
                .GetRequiredService<Navigation.INavigationService>();
            if (navService == null) return;

            await navService.DisplayAlertAsync(
                AppResources.SessionExpiredTitle,
                AppResources.SessionExpiredMessage);

            await navService.GoToLoginRootAsync();
        }
        catch
        {
            // Best-effort.
        }
    }
}
