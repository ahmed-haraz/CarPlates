using CarPlates.Application.Common.Interfaces;
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

    public ProfileViewModel(IAuthenticationService authService)
    {
        _authService = authService;
        Title = "Profile";
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
                Role = user.Role;
                ProfilePhotoUrl = user.ProfilePhotoUrl;
            }
        });
    }

    [RelayCommand]
    private async Task EditProfileAsync()
    {
        await Shell.Current.DisplayAlertAsync("Edit Profile", "Profile editing coming soon", "OK");
    }

    [RelayCommand]
    private async Task ChangePhotoAsync()
    {
        await Shell.Current.DisplayAlertAsync("Change Photo", "Photo upload coming soon", "OK");
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        await Shell.Current.DisplayAlertAsync("Change Password", "Password change coming soon", "OK");
    }
}
