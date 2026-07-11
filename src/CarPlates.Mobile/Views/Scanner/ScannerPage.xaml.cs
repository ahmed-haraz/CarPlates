using CarPlates.Mobile.ViewModels;

namespace CarPlates.Mobile.Views.Scanner;

public partial class ScannerPage : ContentPage
{
    public ScannerPage(ScannerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ScannerViewModel vm)
        {
            _ = Dispatcher.DispatchAsync(() => vm.StartScanningCommand.ExecuteAsync(null));
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is ScannerViewModel vm)
        {
            await vm.StopScanningCommand.ExecuteAsync(null);
        }
    }
}
