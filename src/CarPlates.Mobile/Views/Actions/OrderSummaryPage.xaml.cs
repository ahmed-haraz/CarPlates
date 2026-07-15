using CarPlates.Mobile.ViewModels;

namespace CarPlates.Mobile.Views.Actions;

public partial class OrderSummaryPage : ContentPage
{
    public OrderSummaryPage(NewOrderViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
