using AppTheme = CarPlates.Domain.Enums.AppTheme;
using MauiApp = global::Microsoft.Maui.Controls.Application;

namespace CarPlates.Mobile.Helpers;

public static class ThemeHelper
{
    public static void ApplyTheme(AppTheme theme)
    {
        MauiApp.Current!.UserAppTheme = theme switch
        {
            AppTheme.Dark => AppTheme.Dark,
            AppTheme.Light => AppTheme.Light,
            _ => AppTheme.System
        };
    }

    public static async Task LoadAndApplyThemeAsync(ISettingsService settingsService)
    {
        var theme = await settingsService.GetThemeAsync();
        ApplyTheme(theme);
    }
}
