using CarPlates.API.Common;
using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

// Search/lookup only - the deliberate create-a-customer flow stays inside
// CustomerCarService.RegisterAsync where it's tied to registering a car.
public class CustomerService(ApplicationDbContext context) : ICustomerService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<PagedResult<CustomerDto>> GetAllAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.WhCustomers.AsNoTracking().Where(c => !c.Inactive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                c.Name_Ar.Contains(search) ||
                c.Name_En.Contains(search) ||
                (c.Mobile != null && c.Mobile.Contains(search)) ||
                (c.Phone1 != null && c.Phone1.Contains(search)));
        }

        query = query.OrderBy(c => c.Name_En);

        var paged = await query.ToPagedResultAsync(page, pageSize, cancellationToken);
        var items = paged.Items.Select(MapToDto).ToList();

        return new PagedResult<CustomerDto>(items, paged.TotalCount, paged.Page, paged.PageSize, paged.TotalPages);
    }

    public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await _context.WhCustomers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return customer == null ? null : MapToDto(customer);
    }

    private static CustomerDto MapToDto(WhCustomer c) => new(
        c.Id, c.Code, c.Name_Ar, c.Name_En, c.Mobile, c.Phone1, c.email, c.Address);
}
