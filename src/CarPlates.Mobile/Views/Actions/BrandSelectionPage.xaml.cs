using CarPlates.Mobile.ViewModels;

namespace CarPlates.Mobile.Views.Actions;

public partial class BrandSelectionPage : ContentPage
{
    public BrandSelectionPage(NewOrderViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
