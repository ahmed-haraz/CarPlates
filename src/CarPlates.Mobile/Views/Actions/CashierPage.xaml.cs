
using CarPlates.Mobile.ViewModels;

namespace CarPlates.Mobile.Views.Actions;

public partial class CashierPage : ContentPage
{
    private readonly CashierViewModel _viewModel;

    public CashierPage(CashierViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.Bills.Count == 0)
            await _viewModel.LoadBillsCommand.ExecuteAsync(null);
    }
}
