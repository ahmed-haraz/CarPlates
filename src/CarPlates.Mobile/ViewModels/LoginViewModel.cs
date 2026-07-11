using CarPlates.Application.Authentication.Commands;
using CarPlates.Application.Common.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using CarPlates.Mobile.Localization;
using CarPlates.Mobile.Navigation;

namespace CarPlates.Mobile.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IMediator _mediator;
    private readonly IAuthenticationService _authService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isPasswordVisible;

    public LoginViewModel(IMediator mediator, IAuthenticationService authService, INavigationService navigation) : base(navigation)
    {
        _mediator = mediator;
        _authService = authService;
        Title = AppResources.SignIn;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = AppResources.PleaseEnterCredentials;
            HasError = true;
            return;
        }

        await ExecuteAsync(async () =>
        {
            var command = new LoginCommand(Username, Password);
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
