using CarPlates.Mobile.Localization;
using CarPlates.Mobile.Views.Dashboard;
using CarPlates.Mobile.Views.History;
using CarPlates.Mobile.Views.Scanner;
using CarPlates.Mobile.Views.Settings;

namespace CarPlates.Mobile.Views.Main;

public partial class MainTabbedPage : TabbedPage
{
    public MainTabbedPage(
        DashboardPage dashboardPage,
        ScannerPage scannerPage,
        HistoryPage historyPage,
        SettingsPage settingsPage)
    {
        InitializeComponent();

        var dashboardTab = new NavigationPage(dashboardPage)
        {
            Title = AppResources.Dashboard,
            IconImageSource = "dashboard.png"
        };
        var scannerTab = new NavigationPage(scannerPage)
        {
            Title = AppResources.Scan,
            IconImageSource = "camera.png"
        };
        var historyTab = new NavigationPage(historyPage)
        {
            Title = AppResources.History,
            IconImageSource = "history.png"
        };
        var settingsTab = new NavigationPage(settingsPage)
        {
            Title = AppResources.Settings,
            IconImageSource = "settings.png"
        };

        Children.Add(dashboardTab);
        Children.Add(scannerTab);
        Children.Add(historyTab);
        Children.Add(settingsTab);

        // Tab labels are set once here; if the language changes while the user is
        // already inside the tabbed shell, the labels catch up next time
        // MainTabbedPage is recreated (e.g. next login), not live in-place - a
        // known, minor limitation flagged rather than silently left unhandled.
    }
}
