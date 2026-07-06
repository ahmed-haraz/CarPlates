using CarPlates.Application.Common.Interfaces;
using AppTheme = CarPlates.Domain.Enums.AppTheme;

namespace CarPlates.Mobile.Helpers;

public static class ThemeHelper
{
    public static void ApplyTheme(AppTheme theme)
    {
        Microsoft.Maui.Controls.Application.Current!.UserAppTheme = theme switch
        {
            AppTheme.Dark => Microsoft.Maui.ApplicationModel.AppTheme.Dark,
            AppTheme.Light => Microsoft.Maui.ApplicationModel.AppTheme.Light,
            _ => Microsoft.Maui.ApplicationModel.AppTheme.Unspecified
        };
    }

    public static async Task LoadAndApplyThemeAsync(ISettingsService settingsService)
    {
        var theme = await settingsService.GetThemeAsync();
        ApplyTheme(theme);
    }
}
