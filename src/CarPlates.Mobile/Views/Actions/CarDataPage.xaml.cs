using CarPlates.Mobile.ViewModels;

namespace CarPlates.Mobile.Views.Actions;

public partial class CarDataPage : ContentPage
{
    public CarDataPage(CarDataViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
