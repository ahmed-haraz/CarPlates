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
            Title = "Dashboard",
            IconImageSource = "dashboard.png"
        };
        var scannerTab = new NavigationPage(scannerPage)
        {
            Title = "Scan",
            IconImageSource = "camera.png"
        };
        var historyTab = new NavigationPage(historyPage)
        {
            Title = "History",
            IconImageSource = "history.png"
        };
        var settingsTab = new NavigationPage(settingsPage)
        {
            Title = "Settings",
            IconImageSource = "settings.png"
        };

        Children.Add(dashboardTab);
        Children.Add(scannerTab);
        Children.Add(historyTab);
        Children.Add(settingsTab);
    }
}
