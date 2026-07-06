using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Application.Vehicle.Queries;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace CarPlates.Mobile.ViewModels;

[QueryProperty(nameof(PlateNumber), "plateNumber")]
public partial class VehicleDetailsViewModel : BaseViewModel
{
    private readonly IMediator _mediator;
    private readonly IScanRepository _scanRepository;

    [ObservableProperty]
    private string _plateNumber = string.Empty;

    [ObservableProperty]
    private VehicleDetailsDto? _vehicleDetails;

    [ObservableProperty]
    private List<ScanRecordDto> _scanHistory = new();

    public VehicleDetailsViewModel(IMediator mediator, IScanRepository scanRepository)
    {
        _mediator = mediator;
        _scanRepository = scanRepository;
        Title = "Vehicle Details";
    }

    partial void OnPlateNumberChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            LoadVehicleDetailsAsync();
        }
    }

    [RelayCommand]
    private async Task LoadVehicleDetailsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var query = new GetVehicleDetailsQuery(PlateNumber);
            VehicleDetails = await _mediator.Send(query);

            // Load scan history for this plate
            var scans = await _scanRepository.GetByPlateNumberAsync(PlateNumber);
            // Map to DTOs if needed
        });
    }

    [RelayCommand]
    private async Task ScanAgainAsync()
    {
        await Shell.Current.GoToAsync("//scanner");
    }

    [RelayCommand]
    private async Task ShareAsync()
    {
        if (VehicleDetails == null) return;

        var text = $"Vehicle: {VehicleDetails.Brand} {VehicleDetails.Model}\nPlate: {VehicleDetails.PlateNumber}\nOwner: {VehicleDetails.OwnerName}";
        await Share.RequestAsync(new ShareTextRequest(text));
    }
}
