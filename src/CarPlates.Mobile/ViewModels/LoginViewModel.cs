using CarPlates.Application.Authentication.Commands;
using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using CarPlates.Mobile.Localization;
using CarPlates.Mobile.Navigation;
using Microsoft.Maui.ApplicationModel;

namespace CarPlates.Mobile.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IMediator _mediator;
    private readonly IAuthenticationService _authService;
    private readonly ISettingsService _settingsService;
    private readonly ICompanyApiService _companyApiService;

    [ObservableProperty]
    private string _companyCode = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isPasswordVisible;

    [ObservableProperty]
    private string? _logoUrl;

    private CancellationTokenSource? _logoDebounce;
    private string? _lastFetchedCompanyCode;
    private string? _lastFetchedCompanyName;
    private string? _lastFetchedLogoUrl;

    public bool HasLogo => !string.IsNullOrWhiteSpace(LogoUrl);

    partial void OnLogoUrlChanged(string? value) => OnPropertyChanged(nameof(HasLogo));

    public LoginViewModel(IMediator mediator, IAuthenticationService authService, ISettingsService settingsService, ICompanyApiService companyApiService, INavigationService navigation) : base(navigation)
    {
        _mediator = mediator;
        _authService = authService;
        _settingsService = settingsService;
        _companyApiService = companyApiService;
        Title = AppResources.SignIn;
    }

    /// <summary>Prefills the saved company code so the user doesn't have to retype it.</summary>
    public async Task InitializeAsync()
    {
        CompanyCode = await _settingsService.GetCompanyCodeAsync();
    }

    /// <summary>While the user types a company code, fetch that company's logo (debounced).</summary>
    partial void OnCompanyCodeChanged(string value)
    {
        _logoDebounce?.Cancel();
        var cts = _logoDebounce = new CancellationTokenSource();
        _ = RefreshLogoAsync(value, cts.Token);
    }

    private async Task RefreshLogoAsync(string companyCode, CancellationToken token)
    {
        try
        {
            await Task.Delay(600, token);
            if (token.IsCancellationRequested)
                return;

            if (string.IsNullOrWhiteSpace(companyCode))
            {
                LogoUrl = null;
                return;
            }

            var result = await _companyApiService.GetCompanyInfoAsync(companyCode, token);
            if (token.IsCancellationRequested)
                return;

            _lastFetchedCompanyCode = companyCode;
            _lastFetchedCompanyName = result?.CompanyName;
            _lastFetchedLogoUrl = string.IsNullOrWhiteSpace(result?.LogoUrl) ? null : result.LogoUrl;
            LogoUrl = _lastFetchedLogoUrl;
        }
        catch (OperationCanceledException)
        {
            // Debounced: a newer company code cancelled this one.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Login] Logo fetch failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(CompanyCode))
        {
            ErrorMessage = AppResources.PleaseEnterCompanyCode;
            HasError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = AppResources.PleaseEnterCredentials;
            HasError = true;
            return;
        }

        await ExecuteAsync(async () =>
        {
            // Remember the company code so it's prefilled next time.
            await _settingsService.SetCompanyCodeAsync(CompanyCode);

            // Remember the logo/name fetched for this company so the dashboard can show it.
            if (_lastFetchedCompanyCode == CompanyCode)
            {
                await _settingsService.SetCompanyNameAsync(_lastFetchedCompanyName ?? string.Empty);
                await _settingsService.SetCompanyLogoUrlAsync(_lastFetchedLogoUrl ?? string.Empty);
            }

            var deviceId = await SecureStorage.GetAsync("device_id");
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                deviceId = Guid.NewGuid().ToString();
                await SecureStorage.SetAsync("device_id", deviceId);
            }

            var appVersion = VersionTracking.CurrentVersion;
            var manufacturer = DeviceInfo.Manufacturer;
            var model = DeviceInfo.Model;
            var deviceName = DeviceInfo.Name;

            var deviceInfo = new DeviceInfoDto(CompanyCode, deviceId, appVersion, manufacturer, model, deviceName);

            var command = new LoginCommand(Username, Password, deviceInfo);
            var result = await _mediator.Send(command);

            if (result.Success)
            {
                await Navigation.GoToMainRootAsync();
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? AppResources.LoginFailed;
                HasError = true;
            }
        }, AppResources.LoginFailedTryAgain);
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }
}
