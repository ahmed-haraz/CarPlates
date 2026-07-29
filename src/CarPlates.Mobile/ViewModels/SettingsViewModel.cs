using CarPlates.Application.Authentication.Commands;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Shared.Constants;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CarPlates.Mobile.Localization;
using CarPlates.Mobile.Navigation;
using CarPlates.Mobile.Theming;
using CarPlates.Mobile.Views.About;
using MediatR;
using System.Globalization;
using System.Net.Http.Json;
using CarPlates.Shared.Models;
using AppTheme = CarPlates.Domain.Enums.AppTheme;
using System.Collections.ObjectModel;

namespace CarPlates.Mobile.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly IMediator _mediator;
    private readonly ISettingsService _settingsService;
    private readonly IAuthenticationService _authService;
    private readonly IThemeService _themeService;
    private readonly IApiConnectivityService _connectivityService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPaymentGatewaySettingsApiService _paymentSettingsService;
    private readonly IReceiptPrintService _printService;

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private string _selectedLanguage = "English";

    [ObservableProperty]
    private string _apiUrl = string.Empty;

    [ObservableProperty]
    private bool _isApiUrlLocked = true;

    [ObservableProperty]
    private string _apiUrlPassword = string.Empty;

    [ObservableProperty]
    private float _ocrConfidence = 0.75f;

    [ObservableProperty]
    private bool _autoResume = true;

    [ObservableProperty]
    private bool _notificationsEnabled = true;

    // Payment Gateway settings
    [ObservableProperty]
    private bool _paymentGatewayEnabled;

    [ObservableProperty]
    private string _paymentGatewayName = string.Empty;

    [ObservableProperty]
    private string _paymentMerchantId = string.Empty;

    [ObservableProperty]
    private string _paymentApiKey = string.Empty;

    [ObservableProperty]
    private string _paymentEndpointUrl = string.Empty;

    [ObservableProperty]
    private string _paymentAdditionalSettings = string.Empty;

    // Print settings
    [ObservableProperty] private string _selectedPrinter = string.Empty;
    [ObservableProperty] private string _printerIp = string.Empty;
    [ObservableProperty] private int _selectedPrintFormatIndex;
    [ObservableProperty] private bool _useSavedPrintSettings;
    [ObservableProperty] private bool _isIpEntryEnabled = true;
    [ObservableProperty] private int _selectedPrintLanguageIndex;

    partial void OnSelectedPrinterChanged(string value) => IsIpEntryEnabled = string.IsNullOrWhiteSpace(value);

    public List<string> AvailableLanguages { get; } = new() { "English", "العربية" };
    public ObservableCollection<string> AvailablePrinters { get; } = new();
    [ObservableProperty]
    private bool _hasPrinters;

    public List<string> PrintFormatOptions { get; } = new()
    {
        "ESC/POS Formatted",
        "A4 Page",
        "Receipt via Driver",
        "Plain Text (universal)"
    };

    public List<string> PrintLanguageOptions { get; } = new() { "English", "العربية" };

    public SettingsViewModel(
        IMediator mediator,
        ISettingsService settingsService,
        IAuthenticationService authService,
        IThemeService themeService,
        IApiConnectivityService connectivityService,
        IHttpClientFactory httpClientFactory,
        IPaymentGatewaySettingsApiService paymentSettingsService,
        IReceiptPrintService printService,
        INavigationService navigation) : base(navigation)
    {
        _mediator = mediator;
        _settingsService = settingsService;
        _authService = authService;
        _themeService = themeService;
        _connectivityService = connectivityService;
        _httpClientFactory = httpClientFactory;
        _paymentSettingsService = paymentSettingsService;
        _printService = printService;
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

            // Load payment gateway config from API
            var paymentConfig = await _paymentSettingsService.GetAsync();
            PaymentGatewayEnabled = paymentConfig.IsEnabled;
            PaymentGatewayName = paymentConfig.GatewayName;
            PaymentMerchantId = paymentConfig.MerchantId;
            PaymentApiKey = paymentConfig.ApiKey;
            PaymentEndpointUrl = paymentConfig.EndpointUrl;
            PaymentAdditionalSettings = paymentConfig.AdditionalSettings;

            // Load print settings from Preferences (don't scan printers on load — user clicks Refresh)
            SelectedPrinter = Preferences.Get("print_default_printer", string.Empty);
            PrinterIp = Preferences.Get("print_default_ip", string.Empty);
            SelectedPrintFormatIndex = Preferences.Get("print_default_format", 0);
            UseSavedPrintSettings = Preferences.Get("print_auto_print", false);
            SelectedPrintLanguageIndex = Preferences.Get("print_language", 0);
        });
    }

    [RelayCommand]
    private async Task RefreshPrintersAsync()
    {
        try
        {
            var printers = await _printService.GetAvailablePrintersAsync();
            AvailablePrinters.Clear();
            foreach (var p in printers)
                AvailablePrinters.Add(p);
            HasPrinters = AvailablePrinters.Count > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] RefreshPrinters: {ex.Message}");
            HasPrinters = false;
        }
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

            // Save payment gateway config via API (only if enabled)
            if (PaymentGatewayEnabled)
            {
                try
                {
                    await _paymentSettingsService.SaveAsync(new PaymentGatewayConfig
                    {
                        IsEnabled = true,
                        GatewayName = PaymentGatewayName,
                        MerchantId = PaymentMerchantId,
                        ApiKey = PaymentApiKey,
                        EndpointUrl = PaymentEndpointUrl,
                        AdditionalSettings = PaymentAdditionalSettings
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Settings] Failed to save payment gateway settings: {ex.Message}");
                }
            }

            // Save print settings
            Preferences.Set("print_default_printer", SelectedPrinter ?? string.Empty);
            Preferences.Set("print_default_ip", PrinterIp ?? string.Empty);
            Preferences.Set("print_default_format", SelectedPrintFormatIndex);
            Preferences.Set("print_auto_print", UseSavedPrintSettings);
            Preferences.Set("print_language", SelectedPrintLanguageIndex);

            LocalizationResourceManager.Instance.SetCulture(new CultureInfo(language));
            await Navigation.ApplyCurrentFlowDirectionAsync();
            _themeService.ApplyTheme(theme);

            await Navigation.DisplayAlertAsync(AppResources.Success, AppResources.SettingsSaved);
        });
    }

    [RelayCommand]
    private async Task UnlockApiUrlAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiUrlPassword))
        {
            await Navigation.DisplayAlertAsync(AppResources.Error, "Please enter the settings password");
            return;
        }

        await ExecuteAsync(async () =>
        {
            var client = _httpClientFactory.CreateClient("CarPlatesApi");
            var baseUrl = client.BaseAddress?.ToString()?.TrimEnd('/');
            var apiBase = baseUrl?.Replace("/api/v1", "") ?? "https://online.arkancloud.com:7072";

            var response = await client.PostAsJsonAsync("settings/verify-password",
                new { companyCode = AuthConstants.DefaultCompanyCode, password = ApiUrlPassword });

            if (!response.IsSuccessStatusCode)
            {
                await Navigation.DisplayAlertAsync(AppResources.Error, "Failed to verify password");
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<VerifyResult>();
            if (result?.IsValid == true)
            {
                IsApiUrlLocked = false;
                ApiUrlPassword = string.Empty;
            }
            else
            {
                await Navigation.DisplayAlertAsync(AppResources.Error, "Invalid password");
            }
        });
    }

    [RelayCommand]
    private void LockApiUrl()
    {
        IsApiUrlLocked = true;
        ApiUrlPassword = string.Empty;
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        await ExecuteAsync(async () =>
        {
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

    private record VerifyResult(bool IsValid);
}
