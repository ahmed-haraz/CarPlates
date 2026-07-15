
using CarPlates.Mobile.ViewModels;

namespace CarPlates.Mobile.Views.Actions;

public partial class CashierPage : ContentPage
{
    public CashierPage(CashierViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
