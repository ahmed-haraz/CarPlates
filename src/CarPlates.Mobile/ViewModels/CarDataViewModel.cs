using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Domain.Entities;
using CarPlates.Mobile.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CarPlates.Mobile.ViewModels;

/// <summary>
/// Shown after a successful plate scan/lookup: the vehicle is already known, so the
/// vehicle card is pre-populated and the user only needs to add items/services and
/// submit the order (matches the "vehicle already known" reference design).
/// Reuses <see cref="NewOrderViewModel"/>'s items/notes/signature/submit workflow so
/// both outcome screens (found vs. not found) behave identically past the vehicle step.
/// </summary>
public partial class CarDataViewModel : NewOrderViewModel
{
    [ObservableProperty]
    private VehicleDetailsDto? _scannedVehicle;

    public CarDataViewModel(
        INavigationService navigation,
        ICustomerCarLookupService customerCarLookupService,
        IWorkshopLookupService workshopLookupService,
        ICustomerLookupService customerLookupService,
        IItemLookupService itemLookupService,
        IBillApiService billApiService,
        IBillAttachmentApiService billAttachmentApiService,
        IAuthenticationService authenticationService
    ) : base(navigation, customerCarLookupService, workshopLookupService, customerLookupService, itemLookupService, billApiService, billAttachmentApiService, authenticationService)
    {
        Title = "إضافة سيارة جديدة";
    }

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        base.ApplyQueryAttributes(query);

        if (query.TryGetValue("vehicleInfo", out var value) && value is VehicleDetailsDto dto)
        {
            ScannedVehicle = dto;

            SelectedVehicle = new Vehicle
            {
                Id = Guid.NewGuid().ToString(),
                PlateNumber = dto.PlateNumber,
                Brand = dto.Brand,
                Model = dto.Model,
                Color = dto.Color,
                PlateType = dto.PlateType,
            };

            NewPlateNumber = dto.PlateNumber;
        }
    }
}
