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

        var dashboardTab = CreateIconOnlyTab(dashboardPage, "dashboard.svg", AppResources.Dashboard);
        var scannerTab = CreateIconOnlyTab(scannerPage, "camera.svg", AppResources.Scan);
        var historyTab = CreateIconOnlyTab(historyPage, "history.svg", AppResources.History);
        var settingsTab = CreateIconOnlyTab(settingsPage, "settings.svg", AppResources.Settings);

        Children.Add(dashboardTab);
        Children.Add(scannerTab);
        Children.Add(historyTab);
        Children.Add(settingsTab);

    }

    private static NavigationPage CreateIconOnlyTab(Page page, string icon, string accessibilityName)
    {
        var tab = new NavigationPage(page)
        {
            Title = string.Empty,
            IconImageSource = icon
        };

        AutomationProperties.SetName(tab, accessibilityName);
        AutomationProperties.SetHelpText(tab, accessibilityName);

        return tab;
    }
}
