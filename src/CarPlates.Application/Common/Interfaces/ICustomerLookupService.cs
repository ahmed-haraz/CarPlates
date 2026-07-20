using CarPlates.Application.Common.DTOs;

namespace CarPlates.Application.Common.Interfaces;

public interface ICustomerLookupService
{
    Task<PaginatedResult<CustomerLookupResult>> SearchAsync(string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}

public record CustomerLookupResult(int Id, string Name_Ar, string Name_En, string? Mobile, string? Phone1);
