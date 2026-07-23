using CarPlates.API.Models;

namespace CarPlates.API.Interface;

public interface IVehicleService
{
    Task<VehicleDto?> GetByPlateNumberAsync(string plateNumber);
    Task<VehicleDto?> GetByIdAsync(int id);
    Task<PagedResult<VehicleDto>> GetAllAsync(string? search = null, string? status = null, int page = 1, int pageSize = 20, int? branchId = null, long? userId = null);
    Task<VehicleDto> CreateAsync(VehicleCreateDto dto);
    Task<VehicleDto?> UpdateAsync(int id, VehicleUpdateDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(string plateNumber);
}
