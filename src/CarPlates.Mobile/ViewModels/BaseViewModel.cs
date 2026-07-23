using CarPlates.Mobile.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarPlates.Mobile.ViewModels;

public abstract partial class BaseViewModel(INavigationService navigation) : ObservableObject
{
    protected readonly INavigationService Navigation = navigation;

    // Used by the header back-arrow on pages that swap the app's root (e.g. the
    // scan-outcome screens), since those pages aren't pushed onto a navigation
    // stack and so can't simply be popped.
    [RelayCommand]
    private async Task GoBack() => await Navigation.GoToMainRootAsync();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _title;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isAlertPopupVisible;

    [ObservableProperty]
    private string _alertTitle = string.Empty;

    [ObservableProperty]
    private string _alertMessage = string.Empty;

    protected void ShowAlert(string title, string message)
    {
        AlertTitle = title;
        AlertMessage = message;
        IsAlertPopupVisible = true;
    }

    [RelayCommand]
    private void DismissAlert()
    {
        IsAlertPopupVisible = false;
        AlertMessage = string.Empty;
    }

    protected async Task ExecuteAsync(Func<Task> action, string? errorMessage = null)
    {
        if (IsBusy) return;

        IsBusy = true;
        HasError = false;
        ErrorMessage = null;

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BaseViewModel] Unhandled error: {ex}");
            HasError = true;
            ErrorMessage = errorMessage ?? ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> action, string? errorMessage = null)
    {
        if (IsBusy) return default;

        IsBusy = true;
        HasError = false;
        ErrorMessage = null;

        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BaseViewModel] Unhandled error: {ex}");
            HasError = true;
            ErrorMessage = errorMessage ?? ex.Message;
            return default;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
