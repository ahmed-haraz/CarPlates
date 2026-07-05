using CarPlates.Mobile.ViewModels;

namespace CarPlates.Mobile.Views.Splash;

public partial class SplashPage : ContentPage
{
    public SplashPage(SplashViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is SplashViewModel vm)
        {
            await vm.InitializeCommand.ExecuteAsync(null);
        }
    }
}
