using CarPlates.API.Models;

namespace CarPlates.API.Interface;

public interface IVehicleColorService
{
    Task<List<VehicleColor>> GetAllAsync(CancellationToken cancellationToken = default);
}
