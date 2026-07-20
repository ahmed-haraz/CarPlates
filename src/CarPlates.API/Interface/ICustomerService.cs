using CarPlates.API.Models;

namespace CarPlates.API.Interface;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetAllAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
