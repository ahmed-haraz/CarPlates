using CarPlates.Application.Common.DTOs;
using CarPlates.Mobile.Views.Actions;
using CarPlates.Mobile.Views.Login;
using CarPlates.Mobile.Views.Main;

namespace CarPlates.Mobile.Navigation;

public class NavigationService(IServiceProvider serviceProvider) : INavigationService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private static readonly Dictionary<Type, Type> PageTypeCache = new();

    private static Window CurrentWindow => Microsoft.Maui.Controls.Application.Current!.Windows[0];

    private static INavigation CurrentNavigation => CurrentWindow.Page switch
    {
        TabbedPage { CurrentPage: NavigationPage navPage } => navPage.Navigation,
        TabbedPage tabbed => tabbed.Navigation,
        NavigationPage navPage => navPage.Navigation,
        Page page => page.Navigation,
        _ => throw new InvalidOperationException("App has no navigable root page set yet.")
    };

    public Task GoToLoginRootAsync()
    {
        var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
        NavigationPage.SetHasNavigationBar(loginPage, false);
        var nav = new NavigationPage(loginPage) { FlowDirection = Localization.LocalizationResourceManager.Instance.FlowDirection };
        CurrentWindow.Page = nav;
        return Task.CompletedTask;
    }

    public Task GoToMainRootAsync()
    {
        var mainPage = _serviceProvider.GetRequiredService<MainTabbedPage>();
        mainPage.FlowDirection = Localization.LocalizationResourceManager.Instance.FlowDirection;
        CurrentWindow.Page = mainPage;
        return Task.CompletedTask;
    }

    public Task GoToCashierRootAsync()
    {
        var cashierPage = _serviceProvider.GetRequiredService<CashierPage>();
        cashierPage.FlowDirection = Localization.LocalizationResourceManager.Instance.FlowDirection;
        CurrentWindow.Page = new NavigationPage(cashierPage)
        {
            FlowDirection = Localization.LocalizationResourceManager.Instance.FlowDirection
        };
        return Task.CompletedTask;
    }

    public async Task PushAsync<TViewModel>(IDictionary<string, object>? parameters = null) where TViewModel : ViewModels.BaseViewModel
    {
        var pageType = ResolvePageType(typeof(TViewModel));
        var page = (Page)_serviceProvider.GetRequiredService(pageType);
        page.FlowDirection = Localization.LocalizationResourceManager.Instance.FlowDirection;

        if (parameters != null && parameters.Count > 0 && page.BindingContext is IQueryAttributable queryAware)
        {
            queryAware.ApplyQueryAttributes(parameters);
        }

        await CurrentNavigation.PushAsync(page);
    }

    public async Task PushPageAsync<TPage>() where TPage : Page
    {
        var page = _serviceProvider.GetRequiredService<TPage>();
        page.FlowDirection = Localization.LocalizationResourceManager.Instance.FlowDirection;
        await CurrentNavigation.PushAsync(page);
    }

    public async Task GoBackAsync()
    {
        await CurrentNavigation.PopAsync();
    }

    public Task SwitchTabAsync(MainTab tab)
    {
        if (CurrentWindow.Page is TabbedPage tabbed)
        {
            var index = (int)tab;
            if (index >= 0 && index < tabbed.Children.Count)
            {
                tabbed.CurrentPage = tabbed.Children[index];
            }
        }
        return Task.CompletedTask;
    }

    public Task ApplyCurrentFlowDirectionAsync()
    {
        var page = CurrentWindow.Page;
        if (page != null)
        {
            page.FlowDirection = Localization.LocalizationResourceManager.Instance.FlowDirection;
        }
        return Task.CompletedTask;
    }

    public async Task DisplayAlertAsync(string title, string message, string cancel = "OK")
    {
        var page = CurrentWindow.Page;
        if (page != null) await page.DisplayAlertAsync(title, message, cancel);
    }

    public async Task<bool> DisplayConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No")
    {
        var page = CurrentWindow.Page;
        return page != null && await page.DisplayAlertAsync(title, message, accept, cancel);
    }

    public async Task<string?> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string? placeholder = null)
    {
        var page = CurrentWindow.Page;
        if (page == null) return null;
        return await page.DisplayPromptAsync(title, message, accept, cancel, placeholder: placeholder);
    }

    private static Type ResolvePageType(Type viewModelType)
    {
        if (PageTypeCache.TryGetValue(viewModelType, out var cached)) return cached;

        var baseName = viewModelType.Name.EndsWith("ViewModel")
            ? viewModelType.Name[..^"ViewModel".Length]
            : viewModelType.Name;
        var expectedPageName = baseName + "Page";

        var pageType = typeof(NavigationService).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == expectedPageName && typeof(Page).IsAssignableFrom(t));

        if (pageType == null)
        {
            throw new InvalidOperationException(
                $"No page found matching naming convention '{expectedPageName}' for view model '{viewModelType.Name}'. " +
                "Page classes must be named after their view model (e.g. LoginViewModel -> LoginPage).");
        }

        PageTypeCache[viewModelType] = pageType;
        return pageType;
    }

    public Task GoToCarDataAsync(VehicleDetailsDto vehicleInfo)
    {
        var carDataPage = _serviceProvider.GetRequiredService<Views.Actions.CarDataPage>();
        carDataPage.FlowDirection = Localization.LocalizationResourceManager.Instance.FlowDirection;

        if (carDataPage.BindingContext is IQueryAttributable queryAware)
        {
            queryAware.ApplyQueryAttributes(new Dictionary<string, object> { ["vehicleInfo"] = vehicleInfo });
        }

        CurrentWindow.Page = new NavigationPage(carDataPage)
        {
            FlowDirection = Localization.LocalizationResourceManager.Instance.FlowDirection
        };
        return Task.CompletedTask;
    }

    public Task GoToManualEntryAsync()
    {
        var manualEntryPage = _serviceProvider.GetRequiredService<Views.Scanner.ManualEntryPage>();
        manualEntryPage.FlowDirection = Localization.LocalizationResourceManager.Instance.FlowDirection;
        return CurrentNavigation.PushAsync(manualEntryPage);
    }

    public Task GoToCustomerDataAsync(string? plateNumber = null)
    {
        var newOrderPage = _serviceProvider.GetRequiredService<NewOrderPage>();
        newOrderPage.FlowDirection = Localization.LocalizationResourceManager.Instance.FlowDirection;

        if (!string.IsNullOrWhiteSpace(plateNumber) && newOrderPage.BindingContext is IQueryAttributable queryAware)
        {
            queryAware.ApplyQueryAttributes(new Dictionary<string, object> { ["plateNumber"] = plateNumber });
        }

        CurrentWindow.Page = new NavigationPage(newOrderPage)
        {
            FlowDirection = Localization.LocalizationResourceManager.Instance.FlowDirection
        };
        return Task.CompletedTask;
    }
}
