using CarPlates.Application.Common.DTOs;
using CarPlates.Mobile.ViewModels;

namespace CarPlates.Mobile.Navigation;

/// <summary>
/// Replaces Shell.Current navigation. ViewModels depend on this interface instead
/// of talking to Shell or Page/Application types directly, which also makes them
/// unit-testable without a live MAUI app.
/// </summary>
public interface INavigationService
{
    /// <summary>Swaps the app's root to the (unauthenticated) Login flow and clears any back stack.</summary>
    Task GoToLoginRootAsync();

    /// <summary>Swaps the app's root to the main tabbed experience (post-login/splash) and clears any back stack.</summary>
    Task GoToMainRootAsync();


    /// <summary>Swaps the app's root to the "vehicle found" screen, pre-filled with the
    /// details returned by the plate lookup API (matches the design where the vehicle
    /// card is already known/populated).</summary>
    Task GoToCarDataAsync(VehicleDetailsDto vehicleInfo);

    /// <summary>Swaps the app's root to the manual "add customer / add vehicle" screen,
    /// used when the plate lookup found no matching vehicle. The detected plate number
    /// (if any) is pre-filled so the user doesn't have to retype it.</summary>
    Task GoToCustomerDataAsync(string? plateNumber = null);


    /// <summary>Pushes the page paired with <typeparamref name="TViewModel"/> (by naming convention) onto the active navigation stack.</summary>
    Task PushAsync<TViewModel>(IDictionary<string, object>? parameters = null) where TViewModel : BaseViewModel;

    /// <summary>Pushes a page that has no paired view model (e.g. a static About page).</summary>
    Task PushPageAsync<TPage>() where TPage : Page;

    /// <summary>Pops the top page off the active navigation stack.</summary>
    Task GoBackAsync();

    /// <summary>Switches the visible tab within the main tabbed root.</summary>
    Task SwitchTabAsync(MainTab tab);

    /// <summary>Re-applies the current language's flow direction (LTR/RTL) to whatever
    /// root page is currently showing, without swapping/losing it. Call this right after
    /// changing language while already inside the app (e.g. from Settings).</summary>
    Task ApplyCurrentFlowDirectionAsync();

    Task DisplayAlertAsync(string title, string message, string cancel = "OK");
    Task<bool> DisplayConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No");
    Task<string?> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string? placeholder = null);
}

public enum MainTab
{
    Dashboard = 0,
    Scanner = 1,
    History = 2,
    Settings = 3
}
