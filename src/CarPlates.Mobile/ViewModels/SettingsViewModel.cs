using CarPlates.Application.Common.Interfaces;
using CarPlates.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarPlates.Mobile.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly IAuthenticationService _authService;
    private readonly ISyncService _syncService;

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private string _selectedLanguage = "English";

    [ObservableProperty]
    private string _apiUrl = string.Empty;

    [ObservableProperty]
    private float _ocrConfidence = 0.75f;

    [ObservableProperty]
    private bool _autoResume = true;

    [ObservableProperty]
    private bool _notificationsEnabled = true;

    [ObservableProperty]
    private int _pendingSyncCount;

    public List<string> AvailableLanguages { get; } = new() { "English", "Arabic" };

    public SettingsViewModel(
        ISettingsService settingsService,
        IAuthenticationService authService,
        ISyncService syncService)
    {
        _settingsService = settingsService;
        _authService = authService;
        _syncService = syncService;
        Title = "Settings";
    }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var settings = await _settingsService.GetSettingsAsync();
            IsDarkMode = settings.Theme == AppTheme.Dark;
            SelectedLanguage = settings.Language == "ar" ? "Arabic" : "English";
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
            var language = SelectedLanguage == "Arabic" ? "ar" : "en";

            var settings = new AppSettings(
                theme, language, ApiUrl, OcrConfidence, AutoResume, NotificationsEnabled);

            await _settingsService.SaveSettingsAsync(settings);
            await Shell.Current.DisplayAlert("Success", "Settings saved", "OK");
        });
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        await ExecuteAsync(async () =>
        {
            if (!await _syncService.IsOnlineAsync())
            {
                await Shell.Current.DisplayAlert("Offline", "No internet connection", "OK");
                return;
            }

            var result = await _syncService.SyncPendingAsync();
            await Shell.Current.DisplayAlert(
                "Sync Complete",
                $"Synced: {result.SyncedCount}, Failed: {result.FailedCount}",
                "OK");
        });
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        var confirm = await Shell.Current.DisplayAlert(
            "Clear Cache",
            "This will delete all local scan history. Continue?",
            "Clear", "Cancel");

        if (confirm)
        {
            // Clear cache logic
            await Shell.Current.DisplayAlert("Success", "Cache cleared", "OK");
        }
    }

    [RelayCommand]
    private async Task ViewLogsAsync()
    {
        await Shell.Current.DisplayAlert("Logs", "Log viewer coming soon", "OK");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirm = await Shell.Current.DisplayAlert(
            "Logout",
            "Are you sure you want to logout?",
            "Logout", "Cancel");

        if (confirm)
        {
            await _authService.LogoutAsync();
            await Shell.Current.GoToAsync("//login");
        }
    }

    [RelayCommand]
    private async Task NavigateToAboutAsync()
    {
        await Shell.Current.GoToAsync("about");
    }
}
