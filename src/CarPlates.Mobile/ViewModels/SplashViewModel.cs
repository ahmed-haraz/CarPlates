using CarPlates.Application.Common.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarPlates.Mobile.ViewModels;

public partial class SplashViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authService;

    public SplashViewModel(IAuthenticationService authService)
    {
        _authService = authService;
        Title = "CarPlates";
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        await Task.Delay(2000); // Splash delay

        var isAuthenticated = await _authService.IsAuthenticatedAsync();

        if (isAuthenticated)
        {
            await Shell.Current.GoToAsync("//main/dashboard");
        }
        else
        {
            await Shell.Current.GoToAsync("//login");
        }
    }
}
