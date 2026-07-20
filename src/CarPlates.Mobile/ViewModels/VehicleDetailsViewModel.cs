using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Application.Vehicle.Queries;
using CarPlates.Mobile.Localization;
using CarPlates.Mobile.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using System.Collections.ObjectModel;

namespace CarPlates.Mobile.ViewModels;

public partial class VehicleDetailsViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IMediator _mediator;
    private readonly IScanRepository _scanRepository;
    private readonly ICustomerCarLookupService _customerCarLookupService;
    private readonly ICustomerLookupService _customerLookupService;
    private readonly IItemLookupService _itemLookupService;

    [ObservableProperty]
    private string _plateNumber = string.Empty;

    [ObservableProperty]
    private VehicleDetailsDto? _vehicleDetails;

    [ObservableProperty]
    private List<ScanRecordDto> _scanHistory = new();

    // Reference lookups from the API - available for an edit/reassign-owner flow on this page.
    [ObservableProperty] private ObservableCollection<VehicleTypeResult> _vehicleTypes = new();
    [ObservableProperty] private ObservableCollection<EngineTypeResult> _engineTypes = new();
    [ObservableProperty] private ObservableCollection<CarMakeResult> _makes = new();

    // Customer search - e.g. to confirm/reassign this vehicle's owner.
    [ObservableProperty] private string _customerSearchText = string.Empty;
    [ObservableProperty] private ObservableCollection<CustomerLookupResult> _customerSearchResults = new();

    // Item search - e.g. to start a bill for this vehicle from its details page.
    [ObservableProperty] private string _itemSearchText = string.Empty;
    [ObservableProperty] private ObservableCollection<ItemLookupResult> _itemSearchResults = new();

    public VehicleDetailsViewModel(
        IMediator mediator,
        IScanRepository scanRepository,
        ICustomerCarLookupService customerCarLookupService,
        ICustomerLookupService customerLookupService,
        IItemLookupService itemLookupService,
        INavigationService navigation) : base(navigation)
    {
        _mediator = mediator;
        _scanRepository = scanRepository;
        _customerCarLookupService = customerCarLookupService;
        _customerLookupService = customerLookupService;
        _itemLookupService = itemLookupService;
        Title = AppResources.VehicleDetails;
    }

    // Replaces Shell's [QueryProperty]/routing-based parameter passing now that
    // navigation goes through INavigationService.PushAsync's parameters dictionary.
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("plateNumber", out var value) && value is string plate)
        {
            PlateNumber = plate;
        }
    }

    partial void OnPlateNumberChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _ = LoadVehicleDetailsAsync();
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
            ScanHistory = (await _scanRepository.GetAllByPlateNumberAsync(PlateNumber)).ToList();

            // Load the reference lookups this page needs for an edit/reassign flow, alongside
            // the vehicle itself rather than eagerly on construction.
            if (Makes.Count == 0)
            {
                await LoadLookupsAsync();
            }
        });
    }

    private async Task LoadLookupsAsync()
    {
        var makesTask = _customerCarLookupService.GetMakesAsync();
        var vehicleTypesTask = _customerCarLookupService.GetVehicleTypesAsync();
        var engineTypesTask = _customerCarLookupService.GetEngineTypesAsync();

        await Task.WhenAll(makesTask, vehicleTypesTask, engineTypesTask);

        Makes = new ObservableCollection<CarMakeResult>(makesTask.Result);
        VehicleTypes = new ObservableCollection<VehicleTypeResult>(vehicleTypesTask.Result);
        EngineTypes = new ObservableCollection<EngineTypeResult>(engineTypesTask.Result);
    }

    // Search wh_Customers - e.g. to confirm or reassign this vehicle's registered owner.
    [RelayCommand]
    private async Task SearchCustomerAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _customerLookupService.SearchAsync(CustomerSearchText, pageSize: 20);
            CustomerSearchResults = new ObservableCollection<CustomerLookupResult>(result.Items);
        });
    }

    // Search the item catalog - e.g. to start adding lines to a bill for this vehicle.
    [RelayCommand]
    private async Task SearchItemsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var search = string.IsNullOrWhiteSpace(ItemSearchText) ? null : ItemSearchText;
            var result = await _itemLookupService.SearchAsync(search, pageSize: 20);
            ItemSearchResults = new ObservableCollection<ItemLookupResult>(result.Items);
        });
    }

    [RelayCommand]
    private async Task ScanAgainAsync()
    {
        await Navigation.GoBackAsync();
        await Navigation.SwitchTabAsync(MainTab.Scanner);
    }

    [RelayCommand]
    private async Task ShareAsync()
    {
        if (VehicleDetails == null) return;

        var text = $"Vehicle: {VehicleDetails.Brand} {VehicleDetails.Model}\nPlate: {VehicleDetails.PlateNumber}\nOwner: {VehicleDetails.OwnerName}";
        await Share.RequestAsync(new ShareTextRequest(text));
    }
}
