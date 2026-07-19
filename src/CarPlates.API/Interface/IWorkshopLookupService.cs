using CarPlates.API.Models;

namespace CarPlates.API.Interface;

public interface IWorkshopLookupService
{
    Task<PagedResult<TechnicianDto>> GetTechniciansAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<WorkLocationDto>> GetWorkLocationsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
}
