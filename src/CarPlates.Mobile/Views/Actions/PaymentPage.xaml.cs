using CarPlates.Mobile.ViewModels;

namespace CarPlates.Mobile.Views.Actions;

public partial class PaymentPage : ContentPage
{
    public PaymentPage(PaymentViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
