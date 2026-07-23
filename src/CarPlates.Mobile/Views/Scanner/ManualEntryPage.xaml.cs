using CarPlates.Mobile.ViewModels;

namespace CarPlates.Mobile.Views.Scanner;

public partial class ManualEntryPage : ContentPage
{
    public ManualEntryPage(ManualEntryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
