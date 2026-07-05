using CarPlates.Domain.ValueObjects;
using CarPlates.Mobile.ViewModels;

namespace CarPlates.Mobile.Views.Vehicle;

[QueryProperty(nameof(PlateNumber), "plateNumber")]
public partial class VehicleDetailsPage : ContentPage
{
    public VehicleDetailsPage(VehicleDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is VehicleDetailsViewModel vm)
        {
            await vm.LoadVehicleDetailsCommand.ExecuteAsync(null);
        }
    }
}
