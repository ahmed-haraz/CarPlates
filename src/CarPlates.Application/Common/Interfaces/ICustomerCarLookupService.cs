using CarPlates.Application.Common.DTOs;

namespace CarPlates.Application.Common.Interfaces;

public interface ICustomerCarLookupService
{
    Task<CustomerCarScanResult> ScanAsync(CustomerCarScanRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CarMakeResult>> GetMakesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CarModelResult>> GetModelsAsync(int makeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VehicleTypeResult>> GetVehicleTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EngineTypeResult>> GetEngineTypesAsync(CancellationToken cancellationToken = default);

    Task<PaginatedResult<CarMakeResult>> GetMakesPagedAsync(string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<PaginatedResult<CarModelResult>> GetModelsPagedAsync(int? makeId = null, string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<CarMakeResult> CreateMakeAsync(CreateMakeRequest request, CancellationToken cancellationToken = default);
    Task<CarMakeResult> UpdateMakeAsync(int id, UpdateMakeRequest request, CancellationToken cancellationToken = default);
    Task DeleteMakeAsync(int id, CancellationToken cancellationToken = default);
    Task<CarModelResult> CreateModelAsync(CreateModelRequest request, CancellationToken cancellationToken = default);
    Task<CarModelResult> UpdateModelAsync(int id, UpdateModelRequest request, CancellationToken cancellationToken = default);
    Task DeleteModelAsync(int id, CancellationToken cancellationToken = default);
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
    long? CarHeaderId,
    string? ErrorMessage,
    string? MakeName_Ar = null,
    string? MakeName_En = null,
    string? ModelName_Ar = null,
    string? ModelName_En = null);

public record CarMakeResult(int MakeID, int Code, string Name_Ar, string Name_En, string? IconOriginalURL);

public record CarModelResult(int ModelID, int MakeID, int Code, string Name_Ar, string Name_En);

public record VehicleTypeResult(int Id, string? Name_Ar, string? Name_En);

public record EngineTypeResult(int Id, string? Name_Ar, string? Name_En);

public record CreateMakeRequest(string? Code, string Name_Ar, string Name_En);

public record UpdateMakeRequest(string? Code, string Name_Ar, string Name_En);

public record CreateModelRequest(string? Code, int MakeID, string Name_Ar, string Name_En);

public record UpdateModelRequest(string? Code, int MakeID, string Name_Ar, string Name_En);
