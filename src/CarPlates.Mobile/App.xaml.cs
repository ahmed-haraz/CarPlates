using Microsoft.Extensions.DependencyInjection;

namespace CarPlates.Mobile;

public partial class App : Microsoft.Maui.Controls.Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Root page is resolved via DI (see MauiProgram) so the Splash page's
        // constructor-injected view model works the same way every other page's
        // does. Shell used to own this bootstrapping implicitly; now it's explicit.
        var services = IPlatformApplication.Current!.Services;
        var splashPage = services.GetRequiredService<Views.Splash.SplashPage>();
        NavigationPage.SetHasNavigationBar(splashPage, false);

        var rootNav = new NavigationPage(splashPage);
        return new Window(rootNav);
    }
}
