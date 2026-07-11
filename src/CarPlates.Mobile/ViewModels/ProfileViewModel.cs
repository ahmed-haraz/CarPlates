using CarPlates.Application.Common.Interfaces;
using CarPlates.Mobile.Localization;
using CarPlates.Mobile.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarPlates.Mobile.ViewModels;

public partial class ProfileViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authService;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _role = string.Empty;

    [ObservableProperty]
    private string? _profilePhotoUrl;

    public ProfileViewModel(IAuthenticationService authService, INavigationService navigation) : base(navigation)
    {
        _authService = authService;
        Title = AppResources.Profile;
    }

    [RelayCommand]
    private async Task LoadProfileAsync()
    {
        await ExecuteAsync(async () =>
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user != null)
            {
                UserName = user.Username;
                Email = user.Email;
                FullName = user.FullName;
                // Role and ProfilePhotoUrl aren't part of the legacy fw_Users-backed
                // UserDto returned by the API, so they're left as-is here rather
                // than guessed from unrelated fields like Usertype.
            }
        });
    }

    [RelayCommand]
    private async Task EditProfileAsync()
    {
        await Navigation.DisplayAlertAsync(AppResources.EditProfile, AppResources.EditProfileComingSoon);
    }

    [RelayCommand]
    private async Task ChangePhotoAsync()
    {
        await Navigation.DisplayAlertAsync(AppResources.ChangePhoto, AppResources.ChangePhotoComingSoon);
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        await Navigation.DisplayAlertAsync(AppResources.ChangePassword, AppResources.ChangePasswordComingSoon);
    }
}
