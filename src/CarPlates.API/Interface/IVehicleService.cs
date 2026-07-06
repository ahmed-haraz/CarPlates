using CarPlates.API.Models;

namespace CarPlates.API.Interface;

public interface IVehicleService
{
    Task<VehicleDto?> GetByPlateNumberAsync(string plateNumber);
    Task<VehicleDto?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<VehicleDto>> GetAllAsync(string? search = null, string? status = null);
    Task<VehicleDto> CreateAsync(VehicleCreateDto dto);
    Task<VehicleDto?> UpdateAsync(Guid id, VehicleUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(string plateNumber);
}
