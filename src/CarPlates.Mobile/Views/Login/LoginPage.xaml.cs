using CarPlates.Mobile.ViewModels;

namespace CarPlates.Mobile.Views.Login;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
