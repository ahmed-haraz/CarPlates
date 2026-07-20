namespace CarPlates.Mobile.Helpers;

/// <summary>
/// Legacy helper kept for views that still need page-level dialogs. App navigation
/// is handled by INavigationService/NavigationPage instead of Shell routes.
/// </summary>
public static class NavigationHelper
{
    public static Task GoToAsync(string route, Dictionary<string, object>? parameters = null)
    {
        throw new NotSupportedException(
            $"Shell route navigation ('{route}') is no longer supported. Inject INavigationService and use NavigationPage-based navigation instead.");
    }

    public static async Task ShowAlertAsync(string title, string message)
    {
        var page = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page != null)
        {
            await page.DisplayAlertAsync(title, message, "OK");
        }
    }

    public static async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var page = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page;
        return page != null && await page.DisplayAlertAsync(title, message, "Yes", "No");
    }
}
