using CarPlates.Application.Common.DTOs;

namespace CarPlates.Application.Common.Interfaces;

public interface IWorkshopLookupService
{
    Task<PaginatedResult<TechnicianResult>> GetTechniciansAsync(string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<PaginatedResult<WorkLocationResult>> GetWorkLocationsAsync(string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    Task<TechnicianResult> RegisterTechnicianAsync(CreateTechnicianRequest request, CancellationToken cancellationToken = default);
    Task<TechnicianResult> UpdateTechnicianAsync(int id, UpdateTechnicianRequest request, CancellationToken cancellationToken = default);
    Task DeleteTechnicianAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkLocationResult> RegisterWorkLocationAsync(CreateWorkLocationRequest request, CancellationToken cancellationToken = default);
    Task<WorkLocationResult> UpdateWorkLocationAsync(int id, UpdateWorkLocationRequest request, CancellationToken cancellationToken = default);
    Task DeleteWorkLocationAsync(int id, CancellationToken cancellationToken = default);
}

public record TechnicianResult(int Id, int? Code, string? Name_Ar, string? Name_En);

public record WorkLocationResult(int Id, int? Code, string? Name_Ar, string? Name_En);

public record CreateTechnicianRequest(string? Code, string Name_Ar, string Name_En);

public record UpdateTechnicianRequest(string? Code, string Name_Ar, string Name_En);

public record CreateWorkLocationRequest(string? Code, string Name_Ar, string Name_En);

public record UpdateWorkLocationRequest(string? Code, string Name_Ar, string Name_En);
