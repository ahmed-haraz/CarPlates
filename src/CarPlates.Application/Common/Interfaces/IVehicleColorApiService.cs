using CarPlates.Shared.Models;

namespace CarPlates.Application.Common.Interfaces;

public interface IVehicleColorApiService
{
    Task<List<VehicleColorDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
