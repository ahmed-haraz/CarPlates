using CarPlates.Application.Common.Interfaces;
using AppTheme = CarPlates.Domain.Enums.AppTheme;

namespace CarPlates.Mobile.Helpers;

public static class ThemeHelper
{
    public static void ApplyTheme(AppTheme theme)
    {
        Application.Current!.UserAppTheme = theme switch
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
