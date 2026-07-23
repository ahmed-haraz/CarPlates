using CarPlates.Domain.Entities;

namespace CarPlates.Application.Common.Interfaces;

public interface IVehicleLookupService
{
    Task<VehicleLookupResult> LookupAsync(string plateNumber, CancellationToken cancellationToken = default);
}

public record VehicleLookupResult(
    bool Found,
    string? PlateNumber,
    string? Brand,
    string? Model,
    string? Color,
    string? OwnerName,
    string? AccessStatus,
    string? ErrorMessage,
    string? PlateType = null);
