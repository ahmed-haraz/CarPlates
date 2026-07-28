using CarPlates.API.Models;

namespace CarPlates.API.Interface;

public interface IWorkshopLookupService
{
    Task<PagedResult<TechnicianDto>> GetTechniciansAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<WorkLocationDto>> GetWorkLocationsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<TechnicianDto> RegisterTechnicianAsync(RegisterTechnicianRequestDto request);
    Task<TechnicianDto> UpdateTechnicianAsync(int id, RegisterTechnicianRequestDto request);
    Task DeleteTechnicianAsync(int id);
    Task<WorkLocationDto> RegisterWorkLocationAsync(RegisterWorkLocationRequestDto request);
    Task<WorkLocationDto> UpdateWorkLocationAsync(int id, RegisterWorkLocationRequestDto request);
    Task DeleteWorkLocationAsync(int id);
}
