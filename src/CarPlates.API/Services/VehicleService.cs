using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;


public class VehicleService(ApplicationDbContext context, ICustomerCarService customerCarService) : IVehicleService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ICustomerCarService _customerCarService = customerCarService;

    public async Task<VehicleDto?> GetByPlateNumberAsync(string plateNumber)
    {
        var car = await _context.CustomerCarsFull
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PlateNumber == plateNumber && c.CarStatus != 0);

        return car == null ? null : MapToDto(car);
    }

    public async Task<VehicleDto?> GetByIdAsync(int id)
    {
        var car = await _context.CustomerCarsFull
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.CarStatus != 0);

        return car == null ? null : MapToDto(car);
    }

    public async Task<IReadOnlyList<VehicleDto>> GetAllAsync(string? search = null, string? status = null)
    {
        var query = _context.CustomerCarsFull.AsNoTracking().Where(c => c.CarStatus != 0);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                (c.PlateNumber != null && c.PlateNumber.Contains(search)) ||
                (c.MakeName != null && c.MakeName.Contains(search)) ||
                (c.CustomerName_En != null && c.CustomerName_En.Contains(search)) ||
                (c.CustomerName_Ar != null && c.CustomerName_Ar.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.VehicleStatusName_En == status || c.VehicleStatusName_Ar == status);
        }

        var cars = await query.OrderByDescending(c => c.InsertDateTime).ToListAsync();
        return cars.Select(MapToDto).ToList();
    }

    public async Task<VehicleDto> CreateAsync(VehicleCreateDto dto)
    {
        var makeId = await ResolveMakeIdAsync(dto.Brand);
        var modelId = await ResolveModelIdAsync(dto.Model, makeId);

        // Delegates the actual customer/branch/car creation to CustomerCarService so that
        // logic isn't duplicated.
        var scanDto = new CustomerCarScanDto(
            PlateNumber: dto.PlateNumber,
            BranchID: dto.BranchID,
            VIN: null,
            Color: dto.Color,
            VehicleYear: null,
            CarMakesID: makeId,
            CarModelID: modelId,
            VehicleType: null,
            EngineType: null,
            CustomerName_Ar: null,
            CustomerName_En: dto.OwnerName,
            CustomerMobile: dto.OwnerPhone,
            CustomerPhone1: dto.OwnerPhone);

        var result = await _customerCarService.ScanOrRegisterAsync(scanDto, userId: null);
        return MapToDto(result.Car);
    }

    public async Task<VehicleDto?> UpdateAsync(int id, VehicleUpdateDto dto)
    {
        var car = await _context.CustomerCars.FirstOrDefaultAsync(c => c.Id == id);
        if (car == null || car.Status == 0) return null;

        if (dto.Brand != null)
        {
            car.CarMakesID = await ResolveMakeIdAsync(dto.Brand);
        }

        if (dto.Model != null)
        {
            car.CarModelID = await ResolveModelIdAsync(dto.Model, car.CarMakesID);
        }

        car.Color = dto.Color ?? car.Color;
        car.UpdateDateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        // dto.Notes has no home on wh_CustomerCars — intentionally dropped.

        if (car.CustomerID.HasValue && (dto.OwnerName != null || dto.OwnerPhone != null))
        {
            var customer = await _context.WhCustomers.FirstOrDefaultAsync(c => c.Id == car.CustomerID.Value);
            if (customer != null)
            {
                if (dto.OwnerName != null) customer.Name_En = dto.OwnerName;
                if (dto.OwnerPhone != null)
                {
                    customer.Mobile = dto.OwnerPhone;
                    customer.Phone1 = dto.OwnerPhone;
                }
                customer.UpdateDateTime = car.UpdateDateTime;
            }
        }

        await _context.SaveChangesAsync();

        var full = await _context.CustomerCarsFull.AsNoTracking().FirstAsync(c => c.Id == car.Id);
        return MapToDto(full);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var car = await _context.CustomerCars.FirstOrDefaultAsync(c => c.Id == id);
        if (car == null || car.Status == 0) return false;

        car.Status = 0;
        car.UpdateDateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(string plateNumber)
    {
        return await _context.CustomerCarsFull
            .AnyAsync(c => c.PlateNumber == plateNumber && c.CarStatus != 0);
    }

    private async Task<int?> ResolveMakeIdAsync(string? brand)
    {
        if (string.IsNullOrWhiteSpace(brand)) return null;

        var trimmed = brand.Trim();
        return await _context.CarMakes.AsNoTracking()
            .Where(m => m.MakeName == trimmed)
            .Select(m => (int?)m.MakeID)
            .FirstOrDefaultAsync();
    }

    private async Task<int?> ResolveModelIdAsync(string? model, int? makeId)
    {
        if (string.IsNullOrWhiteSpace(model)) return null;

        var trimmed = model.Trim();
        var query = _context.CarModels.AsNoTracking().Where(m => m.ModelName == trimmed);
        if (makeId.HasValue)
        {
            query = query.Where(m => m.MakeID == makeId.Value);
        }

        return await query.Select(m => (int?)m.ModelID).FirstOrDefaultAsync();
    }

    private static VehicleDto MapToDto(CustomerCarFull c) => new(
        (int)c.Id,
        c.PlateNumber ?? string.Empty,
        c.MakeName,
        c.ModelName,
        c.Color,
        c.CustomerName_En ?? c.CustomerName_Ar);

    private static VehicleDto MapToDto(CustomerCarLookupDto c) => new(
        (int)c.Id,
        c.PlateNumber ?? string.Empty,
        c.MakeName,
        c.ModelName,
        c.Color,
        c.CustomerName_En ?? c.CustomerName_Ar);
}