using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

public class CustomerCarService(ApplicationDbContext context) : ICustomerCarService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<CustomerCarLookupDto?> GetByPlateAsync(string plateNumber)
    {
        var normalized = plateNumber.Trim().ToUpperInvariant();

        var car = await _context.CustomerCarsFull
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PlateNumber == normalized);

        return car == null ? null : MapToDto(car);
    }

    public async Task<IReadOnlyList<CarMakeDto>> GetMakesAsync()
    {
        var makes = await _context.CarMakes
            .AsNoTracking()
            .OrderBy(m => m.MakeName)
            .ToListAsync();

        return [.. makes.Select(m => new CarMakeDto(m.MakeID, m.MakeName))];
    }

    public async Task<IReadOnlyList<CarModelDto>> GetModelsAsync(int makeId)
    {
        var models = await _context.CarModels
            .AsNoTracking()
            .Where(m => m.MakeID == makeId)
            .OrderBy(m => m.ModelName)
            .ToListAsync();

        return [.. models.Select(m => new CarModelDto(m.ModelID, m.MakeID, m.ModelName))];
    }

    public async Task<CustomerCarScanResultDto> ScanOrRegisterAsync(CustomerCarScanDto dto, string? userId)
    {
        var normalizedPlate = dto.PlateNumber.Trim().ToUpperInvariant();
        var userIdLong = long.TryParse(userId, out var uid) ? (long?)uid : null;

        var existing = await _context.CustomerCarsFull
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PlateNumber == normalizedPlate);

        if (existing != null)
        {
            return new CustomerCarScanResultDto(MapToDto(existing), WasNewCar: false, WasNewCustomer: false, WasNewBranchLink: false);
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var wasNewCustomer = false;
        var wasNewBranchLink = false;

        // 1. Find-or-create the customer, matched by mobile number when provided.
        WhCustomer? customer = null;

        if (!string.IsNullOrWhiteSpace(dto.CustomerMobile))
        {
            customer = await _context.WhCustomers
                .FirstOrDefaultAsync(c => c.Mobile == dto.CustomerMobile);
        }

        if (customer == null)
        {
            customer = new WhCustomer
            {
                Code = $"MOB-{now}",
                Name_Ar = string.IsNullOrWhiteSpace(dto.CustomerName_Ar) ? "غير معروف" : dto.CustomerName_Ar,
                Name_En = string.IsNullOrWhiteSpace(dto.CustomerName_En) ? "Unknown" : dto.CustomerName_En,
                Mobile = dto.CustomerMobile,
                Phone1 = dto.CustomerPhone1,
                StoreID = dto.BranchID,
                InsertUserID = userIdLong,
                InsertDateTime = now,
            };

            _context.WhCustomers.Add(customer);
            await _context.SaveChangesAsync();
            wasNewCustomer = true;
        }

        // 2. Find-or-create the branch link for this customer.
        var branchLink = await _context.CustomerBranches
            .FirstOrDefaultAsync(b => b.ParentID == customer.Id && b.BranchID == dto.BranchID);

        if (branchLink == null)
        {
            branchLink = new CustomerBranch
            {
                ParentID = customer.Id,
                BranchID = dto.BranchID,
                InsertUserID = userIdLong,
                InsertDateTime = now,
            };

            _context.CustomerBranches.Add(branchLink);
            await _context.SaveChangesAsync();
            wasNewBranchLink = true;
        }

        // 3. Create the car.
        var car = new CustomerCar
        {
            CustomerID = customer.Id,
            PlateNumber = normalizedPlate,
            VIN = dto.VIN,
            Color = dto.Color,
            VehicleYear = dto.VehicleYear,
            CarMakesID = dto.CarMakesID,
            CarModelID = dto.CarModelID,
            VehicleType = dto.VehicleType,
            EngineType = dto.EngineType,
            Status = 1,
            InsertUserID = userIdLong,
            InsertDateTime = now,
        };

        _context.CustomerCars.Add(car);
        await _context.SaveChangesAsync();

        var full = await _context.CustomerCarsFull.AsNoTracking().FirstAsync(c => c.Id == car.Id);

        return new CustomerCarScanResultDto(MapToDto(full), WasNewCar: true, wasNewCustomer, wasNewBranchLink);
    }

    private static CustomerCarLookupDto MapToDto(CustomerCarFull c) => new(
        c.Id,
        c.PlateNumber,
        c.VIN,
        c.Color,
        c.VehicleYear,
        c.CarMakesID,
        c.MakeName,
        c.CarModelID,
        c.ModelName,
        c.VehicleTypeID,
        c.VehicleTypeName_En,
        c.VehicleTypeName_Ar,
        c.VehicleStatusID,
        c.VehicleStatusName_En,
        c.VehicleStatusName_Ar,
        c.EngineTypeID,
        c.EngineTypeName_En,
        c.EngineTypeName_Ar,
        c.CustomerID,
        c.CustomerCode,
        c.CustomerName_Ar,
        c.CustomerName_En,
        c.CustomerPhone1,
        c.CustomerMobile,
        c.CustomerEmail);
}
