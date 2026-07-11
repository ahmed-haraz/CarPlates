namespace CarPlates.Mobile.Helpers;

public static class NavigationHelper
{
    public static async Task GoToAsync(string route, Dictionary<string, object>? parameters = null)
    {
        if (parameters != null && parameters.Any())
        {
            var query = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value.ToString()!)}"));
            await Shell.Current.GoToAsync($"{route}?{query}");
        }
        else
        {
            await Shell.Current.GoToAsync(route);
        }
    }

    public static async Task DisplayErrorAsync(string title, string message)
    {
        await Shell.Current.DisplayAlertAsync(title, message, "OK");
    }

    public static async Task<bool> DisplayConfirmAsync(string title, string message)
    {
        return await Shell.Current.DisplayAlertAsync(title, message, "Yes", "No");
    }
}
