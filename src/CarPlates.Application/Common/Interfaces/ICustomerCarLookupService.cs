namespace CarPlates.Application.Common.Interfaces;

public interface ICustomerCarLookupService
{
    Task<CustomerCarScanResult> ScanAsync(CustomerCarScanRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CarMakeResult>> GetMakesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CarModelResult>> GetModelsAsync(int makeId, CancellationToken cancellationToken = default);
}

public record CustomerCarScanRequest(
    string PlateNumber,
    int BranchID,
    string? VIN = null,
    string? Color = null,
    int? VehicleYear = null,
    int? CarMakesID = null,
    int? CarModelID = null,
    int? VehicleType = null,
    int? EngineType = null,
    string? CustomerName_Ar = null,
    string? CustomerName_En = null,
    string? CustomerMobile = null,
    string? CustomerPhone1 = null);

public record CustomerCarScanResult(
    bool Success,
    string? PlateNumber,
    string? MakeName,
    string? ModelName,
    string? Color,
    int? VehicleYear,
    string? CustomerName_Ar,
    string? CustomerName_En,
    string? CustomerMobile,
    bool WasNewCar,
    bool WasNewCustomer,
    bool WasNewBranchLink,
    string? ErrorMessage);

public record CarMakeResult(int MakeID, string MakeName);

public record CarModelResult(int ModelID, int MakeID, string ModelName);
