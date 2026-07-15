using CarPlates.Mobile.ViewModels;

namespace CarPlates.Mobile.Views.Actions;

public partial class NewOrderPage : ContentPage
{
    public NewOrderPage(NewOrderViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
