using CarPlates.Application.Authentication.Commands;
using CarPlates.Application.Common.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CarPlates.Mobile.Localization;
using CarPlates.Mobile.Navigation;
using CarPlates.Mobile.Theming;
using CarPlates.Mobile.Views.About;
using MediatR;
using System.Globalization;
using AppTheme = CarPlates.Domain.Enums.AppTheme;

namespace CarPlates.Mobile.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly IMediator _mediator;
    private readonly ISettingsService _settingsService;
    private readonly IAuthenticationService _authService;
    private readonly IThemeService _themeService;
    private readonly IApiConnectivityService _connectivityService;

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private string _selectedLanguage = "English";  // native-language label, matches AvailableLanguages

    [ObservableProperty]
    private string _apiUrl = string.Empty;

    [ObservableProperty]
    private float _ocrConfidence = 0.75f;

    [ObservableProperty]
    private bool _autoResume = true;

    [ObservableProperty]
    private bool _notificationsEnabled = true;

    public List<string> AvailableLanguages { get; } = new() { "English", "العربية" };

    public SettingsViewModel(
        IMediator mediator,
        ISettingsService settingsService,
        IAuthenticationService authService,
        IThemeService themeService,
        IApiConnectivityService connectivityService,
        INavigationService navigation) : base(navigation)
    {
        _mediator = mediator;
        _settingsService = settingsService;
        _authService = authService;
        _themeService = themeService;
        _connectivityService = connectivityService;
        Title = AppResources.Settings;
    }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var settings = await _settingsService.GetSettingsAsync();
            IsDarkMode = settings.Theme == AppTheme.Dark;
            SelectedLanguage = settings.Language == "ar" ? "العربية" : "English";
            ApiUrl = settings.ApiUrl;
            OcrConfidence = settings.OcrConfidence;
            AutoResume = settings.AutoResume;
            NotificationsEnabled = settings.NotificationsEnabled;
        });
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var theme = IsDarkMode ? AppTheme.Dark : AppTheme.Light;
            var language = SelectedLanguage == "العربية" ? "ar" : "en";

            var settings = new AppSettings(
                theme, language, ApiUrl, OcrConfidence, AutoResume, NotificationsEnabled);

            await _settingsService.SaveSettingsAsync(settings);

            // Apply immediately - no restart needed. Switches every {loc:Translate}
            // binding and AppResources.* call in one shot, and flips the current
            // root's FlowDirection for Arabic/English RTL vs LTR layout.
            LocalizationResourceManager.Instance.SetCulture(new CultureInfo(language));
            await Navigation.ApplyCurrentFlowDirectionAsync();
            _themeService.ApplyTheme(theme);

            await Navigation.DisplayAlertAsync(AppResources.Success, AppResources.SettingsSaved);
        });
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Uses whatever is currently typed in the ApiUrl field, saving it
            // first so the test reflects exactly what "Save Settings" would
            // apply - avoids a confusing "it worked here but not after I saved" gap.
            await _settingsService.SetApiUrlAsync(ApiUrl);
            var result = await _connectivityService.TestConnectionAsync();

            if (result.IsReachable)
            {
                await Navigation.DisplayAlertAsync(AppResources.Success, AppResources.ConnectionSuccessful);
            }
            else
            {
                await Navigation.DisplayAlertAsync(AppResources.ConnectionFailed, result.ErrorMessage ?? AppResources.ConnectionFailed);
            }
        });
    }

    [RelayCommand]
    private async Task ViewLogsAsync()
    {
        await Navigation.DisplayAlertAsync(AppResources.ViewLogs, AppResources.LogViewerComingSoon);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirm = await Navigation.DisplayConfirmAsync(
            AppResources.Logout,
            AppResources.LogoutConfirmMessage,
            AppResources.Logout, AppResources.Cancel);

        if (confirm)
        {
            await ExecuteAsync(async () =>
            {
                await _mediator.Send(new LogoutCommand());
            });
            await Navigation.GoToLoginRootAsync();
        }
    }

    [RelayCommand]
    private async Task NavigateToAboutAsync()
    {
        await Navigation.PushPageAsync<AboutPage>();
    }
}
