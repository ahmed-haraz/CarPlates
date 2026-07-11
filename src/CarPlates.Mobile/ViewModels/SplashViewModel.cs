using CarPlates.Application.Common.Interfaces;
using CarPlates.Mobile.Localization;
using CarPlates.Mobile.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarPlates.Mobile.ViewModels;

public partial class SplashViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authService;

    public SplashViewModel(IAuthenticationService authService, INavigationService navigation) : base(navigation)
    {
        _authService = authService;
        Title = AppResources.AppName;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        // Keep startup responsive; do not make users wait on a fixed splash delay.
        await Task.Yield();

        var isAuthenticated = await _authService.IsAuthenticatedAsync();

        if (isAuthenticated)
        {
            await Navigation.GoToMainRootAsync();
        }
        else
        {
            await Navigation.GoToLoginRootAsync();
        }
    }
}
