using AppTheme = CarPlates.Domain.Enums.AppTheme;

namespace CarPlates.Mobile.Theming;

public interface IThemeService
{
    void ApplyTheme(AppTheme theme);
}
