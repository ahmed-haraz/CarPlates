using CarPlates.API.Models;

namespace CarPlates.API.Interface;

public interface ICustomerCarService
{
    Task<CustomerCarLookupDto?> GetByPlateAsync(string plateNumber);
    Task<IReadOnlyList<CarMakeDto>> GetMakesAsync();
    Task<IReadOnlyList<CarModelDto>> GetModelsAsync(int makeId);

    /// <summary>
    /// Looks the plate up in VW_WH_CustomerCarsFull. If it's already registered, returns it
    /// as-is. If not, registers it: finds-or-creates the wh_Customers row (matched by mobile),
    /// finds-or-creates the wh_CustomersBranch link (CustomerID + BranchID), then inserts the
    /// wh_CustomerCars row.
    /// </summary>
    Task<CustomerCarScanResultDto> ScanOrRegisterAsync(CustomerCarScanDto dto, string? userId);
}
