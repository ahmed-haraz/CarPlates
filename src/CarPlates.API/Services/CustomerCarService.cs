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
            .OrderBy(m => m.Name_en ?? m.Name_ar)
            .ToListAsync();

        return [.. makes.Select(m => new CarMakeDto(m.MakeID, m.Code, m.Name_ar, m.Name_en, m.IconOriginalURL))];
    }

    public async Task<IReadOnlyList<CarModelDto>> GetModelsAsync(int makeId)
    {
        var models = await _context.CarModels
            .AsNoTracking()
            .Where(m => m.MakeID == makeId)
            .OrderBy(m => m.Name_en ?? m.Name_ar)
            .ToListAsync();

        return [.. models.Select(m => new CarModelDto(m.ModelID, m.MakeID, m.Code, m.Name_ar, m.Name_en))];
    }

    public async Task<IReadOnlyList<VehicleTypeDto>> GetVehicleTypesAsync()
    {
        var types = await _context.VehicleTypes
            .AsNoTracking()
            .Where(v => v.Status == 1)
            .OrderBy(v => v.Name_en ?? v.Name_ar)
            .ToListAsync();

        return [.. types.Select(v => new VehicleTypeDto(v.Id, v.Code, v.Name_ar, v.Name_en))];
    }

    public async Task<IReadOnlyList<EngineTypeDto>> GetEngineTypesAsync()
    {
        var types = await _context.EngineTypes
            .AsNoTracking()
            .Where(v => v.Status == 1)
            .OrderBy(v => v.Name_en ?? v.Name_ar)
            .ToListAsync();

        return [.. types.Select(v => new EngineTypeDto(v.Id, v.Code, v.Name_ar, v.Name_en))];
    }

    public async Task<CustomerCarScanResultDto> ScanAsync(CustomerCarScanDto dto, string? userId)
    {
        var normalizedPlate = dto.PlateNumber.Trim().ToUpperInvariant();

        var existing = await _context.CustomerCarsFull
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PlateNumber == normalizedPlate);

        if (existing != null)
        {
            return new CustomerCarScanResultDto(MapToDto(existing), WasNewCar: false, WasNewCustomer: false, WasNewBranchLink: false);
        }

        // Unregistered plate: no wh_Customers/wh_CustomersBranch/wh_CustomerCars rows are
        // created here anymore. The scan is still recorded, but only in wh_ScanRecords via
        // the separate scans endpoint - registering a customer/car is now a deliberate,
        // separate action (RegisterAsync below) rather than an automatic side effect of
        // scanning.
        return new CustomerCarScanResultDto(Car: null, WasNewCar: false, WasNewCustomer: false, WasNewBranchLink: false);
    }

    public async Task<CustomerCarScanResultDto> RegisterAsync(CustomerCarScanDto dto, string? userId)
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

        var now = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
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
            var allCodes = await _context.WhCustomers
                .Where(c => c.Code != null && c.Code.All(char.IsDigit))
                .Select(c => c.Code)
                .ToListAsync();
            var maxNum = allCodes.Select(c => int.TryParse(c, out var n) ? n : 0).DefaultIfEmpty(0).Max();
            customer = new WhCustomer
            {
                Code = (maxNum + 1).ToString(),
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
            PlateType = dto.PlateType,
            Status = 1,
            InsertUserID = userIdLong,
            InsertDateTime = now,
        };

        _context.CustomerCars.Add(car);
        await _context.SaveChangesAsync();

        var full = await _context.CustomerCarsFull.AsNoTracking().FirstAsync(c => c.Id == car.Id);

        return new CustomerCarScanResultDto(MapToDto(full), WasNewCar: true, wasNewCustomer, wasNewBranchLink);
    }

    private static bool ShouldAutoGen(string? code) =>
        string.IsNullOrWhiteSpace(code) || code == "*";

    private async Task<int> ResolveMakeCodeAsync(string? code)
    {
        if (!ShouldAutoGen(code) && int.TryParse(code, out var parsed)) return parsed;
        return await _context.CarMakes.MaxAsync(m => (int?)m.Code) + 1 ?? 1;
    }

    private async Task<int> ResolveModelCodeAsync(string? code)
    {
        if (!ShouldAutoGen(code) && int.TryParse(code, out var parsed)) return parsed;
        return await _context.CarModels.MaxAsync(m => (int?)m.Code) + 1 ?? 1;
    }

    public async Task<CarMakeDto> CreateMakeAsync(RegisterCarMakeRequestDto request)
    {
        var code = await ResolveMakeCodeAsync(request.Code);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var make = new CarMake
        {
            Code = code,
            Name_ar = request.Name_ar,
            Name_en = request.Name_en,
            Status = 1,
            InsertDateTime = now,
            UpdateDateTime = now,
        };

        _context.CarMakes.Add(make);
        await _context.SaveChangesAsync();

        return new CarMakeDto(make.MakeID, make.Code, make.Name_ar, make.Name_en, make.IconOriginalURL);
    }

    public async Task<CarMakeDto> UpdateMakeAsync(int id, RegisterCarMakeRequestDto request)
    {
        var make = await _context.CarMakes.FindAsync(id)
            ?? throw new KeyNotFoundException($"CarMake with ID {id} not found.");

        make.Code = await ResolveMakeCodeAsync(request.Code);
        make.Name_ar = request.Name_ar;
        make.Name_en = request.Name_en;
        make.UpdateDateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await _context.SaveChangesAsync();

        return new CarMakeDto(make.MakeID, make.Code, make.Name_ar, make.Name_en, make.IconOriginalURL);
    }

    public async Task DeleteMakeAsync(int id)
    {
        var make = await _context.CarMakes.FindAsync(id);
        if (make != null)
        {
            make.Status = 0;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<CarModelDto> CreateModelAsync(RegisterCarModelRequestDto request)
    {
        var code = await ResolveModelCodeAsync(request.Code);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var model = new CarModel
        {
            MakeID = request.MakeID,
            Code = code,
            Name_ar = request.Name_ar,
            Name_en = request.Name_en,
            Status = 1,
            InsertDateTime = now,
            UpdateDateTime = now,
        };

        _context.CarModels.Add(model);
        await _context.SaveChangesAsync();

        return new CarModelDto(model.ModelID, model.MakeID, model.Code, model.Name_ar, model.Name_en);
    }

    public async Task<CarModelDto> UpdateModelAsync(int id, RegisterCarModelRequestDto request)
    {
        var model = await _context.CarModels.FindAsync(id)
            ?? throw new KeyNotFoundException($"CarModel with ID {id} not found.");

        model.MakeID = request.MakeID;
        model.Code = await ResolveModelCodeAsync(request.Code);
        model.Name_ar = request.Name_ar;
        model.Name_en = request.Name_en;
        model.UpdateDateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await _context.SaveChangesAsync();

        return new CarModelDto(model.ModelID, model.MakeID, model.Code, model.Name_ar, model.Name_en);
    }

    public async Task DeleteModelAsync(int id)
    {
        var model = await _context.CarModels.FindAsync(id);
        if (model != null)
        {
            model.Status = 0;
            await _context.SaveChangesAsync();
        }
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
        c.CustomerEmail,
        c.PlateType);
}
