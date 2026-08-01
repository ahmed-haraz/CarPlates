using CarPlates.API.Models;

namespace CarPlates.API.Interface;

public interface IWorkshopLookupService
{
    Task<PagedResult<TechnicianDto>> GetTechniciansAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<WorkLocationDto>> GetWorkLocationsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<TechnicianDto> RegisterTechnicianAsync(RegisterTechnicianRequestDto request, long? userId = null);
    Task<TechnicianDto> UpdateTechnicianAsync(int id, RegisterTechnicianRequestDto request, long? userId = null);
    Task DeleteTechnicianAsync(int id, long? userId = null);
    Task<WorkLocationDto> RegisterWorkLocationAsync(RegisterWorkLocationRequestDto request, long? userId = null);
    Task<WorkLocationDto> UpdateWorkLocationAsync(int id, RegisterWorkLocationRequestDto request, long? userId = null);
    Task DeleteWorkLocationAsync(int id, long? userId = null);
}
