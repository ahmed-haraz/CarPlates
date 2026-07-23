using CarPlates.Mobile.ViewModels;

namespace CarPlates.Mobile.Views.Actions;

public partial class BrandSelectionPage : ContentPage
{
    public BrandSelectionPage(NewOrderViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is NewOrderViewModel vm)
        {
            vm.BrandSearchText = string.Empty;
            vm.ResetBrandPaging();
        }
    }
}
