using CarPlates.Application.Common.DTOs;

namespace CarPlates.Application.Common.Interfaces;

public interface IWorkshopLookupService
{
    Task<PaginatedResult<TechnicianResult>> GetTechniciansAsync(string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<PaginatedResult<WorkLocationResult>> GetWorkLocationsAsync(string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}

public record TechnicianResult(int Id, string? Name_Ar, string? Name_En);

public record WorkLocationResult(int Id, string? Name_Ar, string? Name_En);
