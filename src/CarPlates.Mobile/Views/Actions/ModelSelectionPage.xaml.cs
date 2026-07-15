
using CarPlates.Mobile.ViewModels;

namespace CarPlates.Mobile.Views.Actions;

public partial class ModelSelectionPage : ContentPage
{
    public ModelSelectionPage(NewOrderViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
