using CarPlates.Application.Common.DTOs;

namespace CarPlates.Application.Common.Interfaces;

public interface ICustomerLookupService
{
    Task<PaginatedResult<CustomerLookupResult>> SearchAsync(string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task UpdateCustomerAsync(int id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task DeleteCustomerAsync(int id, CancellationToken cancellationToken = default);
}

public record CustomerLookupResult(int Id, string Code, string Name_Ar, string Name_En, string? Mobile, string? Phone1, string? Email, string? Address);

public record UpdateCustomerRequest(string Name_Ar, string Name_En, string? Mobile, string? Phone1, string? Email, string? Address);
