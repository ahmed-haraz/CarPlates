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

    public async Task<CustomerDto> UpdateAsync(int id, UpdateCustomerRequestDto request)
    {
        var customer = await _context.WhCustomers.FindAsync(id)
            ?? throw new KeyNotFoundException($"Customer with ID {id} not found.");

        customer.Name_Ar = request.Name_Ar;
        customer.Name_En = request.Name_En;
        customer.Mobile = request.Mobile;
        customer.Phone1 = request.Phone1;
        customer.email = request.Email;
        customer.Address = request.Address;
        customer.UpdateDateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await _context.SaveChangesAsync();

        return MapToDto(customer);
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await _context.WhCustomers.FindAsync(id);
        if (customer != null)
        {
            customer.Inactive = true;
            customer.Status = 0;
            customer.UpdateDateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await _context.SaveChangesAsync();
        }
    }

    private static CustomerDto MapToDto(WhCustomer c) => new(
        c.Id, c.Code, c.Name_Ar, c.Name_En, c.Mobile, c.Phone1, c.email, c.Address);
}
