using AppTheme = CarPlates.Domain.Enums.AppTheme;

namespace CarPlates.Mobile.Theming;

public class ThemeService : IThemeService
{
    private AppTheme _requestedTheme = AppTheme.System;
    private bool _subscribedToOsChanges;

    public void ApplyTheme(AppTheme theme)
    {
        _requestedTheme = theme;
        ApplyEffectiveTheme();

        // Only need one subscription for the lifetime of the app - if the user
        // picks "System", we still want to react live if they flip their OS
        // theme while the app is open (matches how "System" should behave).
        if (!_subscribedToOsChanges)
        {
            Microsoft.Maui.Controls.Application.Current!.RequestedThemeChanged += (_, _) =>
            {
                if (_requestedTheme == AppTheme.System)
                {
                    ApplyEffectiveTheme();
                }
            };
            _subscribedToOsChanges = true;
        }
    }

    private void ApplyEffectiveTheme()
    {
        var app = Microsoft.Maui.Controls.Application.Current;
        if (app == null) return;

        var effectiveDark = _requestedTheme switch
        {
            AppTheme.Dark => true,
            AppTheme.Light => false,
            _ => app.RequestedTheme == Microsoft.Maui.ApplicationModel.AppTheme.Dark
        };

        app.UserAppTheme = _requestedTheme switch
        {
            AppTheme.Dark => Microsoft.Maui.ApplicationModel.AppTheme.Dark,
            AppTheme.Light => Microsoft.Maui.ApplicationModel.AppTheme.Light,
            _ => Microsoft.Maui.ApplicationModel.AppTheme.Unspecified
        };

        var resources = app.Resources;
        var suffix = effectiveDark ? "Dark" : "Light";

        resources["Background"] = (Color)resources[$"Background{suffix}"];
        resources["Surface"] = (Color)resources[$"Surface{suffix}"];
        resources["SurfaceMuted"] = (Color)resources[$"SurfaceMuted{suffix}"];
        resources["TextPrimary"] = (Color)resources[$"TextPrimary{suffix}"];
        resources["TextSecondary"] = (Color)resources[$"TextSecondary{suffix}"];
        resources["Border"] = (Color)resources[$"Border{suffix}"];
    }
}
