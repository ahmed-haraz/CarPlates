using CarPlates.API.Models;

namespace CarPlates.API.Interface;

public interface ICustomerCarService
{
    Task<CustomerCarLookupDto?> GetByPlateAsync(string plateNumber);
    Task<IReadOnlyList<CarMakeDto>> GetMakesAsync();
    Task<IReadOnlyList<CarModelDto>> GetModelsAsync(int makeId);
    Task<IReadOnlyList<VehicleTypeDto>> GetVehicleTypesAsync();
    Task<IReadOnlyList<EngineTypeDto>> GetEngineTypesAsync();

    /// <summary>
    /// Looks the plate up in VW_WH_CustomerCarsFull. If it's already registered, returns it
    /// as-is (WasNewCar/WasNewCustomer/WasNewBranchLink all false). If not, returns a result
    /// with Car = null and all flags false - scanning an unregistered plate no longer creates
    /// wh_Customers/wh_CustomersBranch/wh_CustomerCars rows. The scan itself is still recorded,
    /// but only in wh_ScanRecords (via the separate scans endpoint).
    /// </summary>
    Task<CustomerCarScanResultDto> ScanAsync(CustomerCarScanDto dto, string? userId);

    /// <summary>
    /// Deliberate registration entry point (e.g. the "create vehicle" flow, not the scanner):
    /// if the plate is already registered, returns it as-is; otherwise finds-or-creates the
    /// wh_Customers row (matched by mobile), finds-or-creates the wh_CustomersBranch link, and
    /// inserts the wh_CustomerCars row. Unlike ScanAsync, this one is expected to write data.
    /// </summary>
    Task<CustomerCarScanResultDto> RegisterAsync(CustomerCarScanDto dto, string? userId);
}
