using CarPlates.Application.Authentication.Commands;
using CarPlates.Application.Common.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CarPlates.Mobile.Navigation;
using CarPlates.Mobile.Views.About;
using MediatR;
using AppTheme = CarPlates.Domain.Enums.AppTheme;

namespace CarPlates.Mobile.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly IMediator _mediator;
    private readonly ISettingsService _settingsService;
    private readonly IAuthenticationService _authService;
    private readonly ISyncService _syncService;
    private readonly IScanRepository _scanRepository;
    private readonly IPendingUploadRepository _pendingUploadRepository;

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
        IMediator mediator,
        ISettingsService settingsService,
        IAuthenticationService authService,
        ISyncService syncService,
        IScanRepository scanRepository,
        IPendingUploadRepository pendingUploadRepository,
        INavigationService navigation) : base(navigation)
    {
        _mediator = mediator;
        _settingsService = settingsService;
        _authService = authService;
        _syncService = syncService;
        _scanRepository = scanRepository;
        _pendingUploadRepository = pendingUploadRepository;
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
            await Navigation.DisplayAlertAsync("Success", "Settings saved");
        });
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        await ExecuteAsync(async () =>
        {
            if (!await _syncService.IsOnlineAsync())
            {
                await Navigation.DisplayAlertAsync("Offline", "No internet connection");
                return;
            }

            var result = await _syncService.SyncPendingAsync();
            await Navigation.DisplayAlertAsync(
                "Sync Complete",
                $"Synced: {result.SyncedCount}, Failed: {result.FailedCount}");
        });
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        var confirm = await Navigation.DisplayConfirmAsync(
            "Clear Cache",
            "This will delete all local scan history. Continue?",
            "Clear", "Cancel");

        if (confirm)
        {
            await ExecuteAsync(async () =>
            {
                await _scanRepository.ClearAllAsync();
                await _pendingUploadRepository.ClearAllAsync();
                PendingSyncCount = 0;
            });
            await Navigation.DisplayAlertAsync("Success", "Cache cleared");
        }
    }

    [RelayCommand]
    private async Task ViewLogsAsync()
    {
        await Navigation.DisplayAlertAsync("Logs", "Log viewer coming soon");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirm = await Navigation.DisplayConfirmAsync(
            "Logout",
            "Are you sure you want to logout?",
            "Logout", "Cancel");

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
