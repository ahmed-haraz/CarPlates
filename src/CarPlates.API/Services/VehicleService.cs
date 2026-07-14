using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

public class VehicleService(ApplicationDbContext context) : IVehicleService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<VehicleDto?> GetByPlateNumberAsync(string plateNumber)
    {
        var vehicle = await _context.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.PlateNumber == plateNumber && !v.IsDeleted);

        return vehicle == null ? null : MapToDto(vehicle);
    }

    public async Task<VehicleDto?> GetByIdAsync(int id)
    {
        var vehicle = await _context.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        return vehicle == null ? null : MapToDto(vehicle);
    }

    public async Task<IReadOnlyList<VehicleDto>> GetAllAsync(string? search = null, string? status = null)
    {
        var query = _context.Vehicles.AsNoTracking().Where(v => !v.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(v => v.PlateNumber.Contains(search) ||
                                     (v.Brand != null && v.Brand.Contains(search)) ||
                                     (v.OwnerName != null && v.OwnerName.Contains(search)));
        }

       
        var vehicles = await query.OrderByDescending(v => v.CreatedAt).ToListAsync();
        return vehicles.Select(MapToDto).ToList();
    }

    public async Task<VehicleDto> CreateAsync(VehicleCreateDto dto)
    {
        var vehicle = new Vehicle
        {
            PlateNumber = dto.PlateNumber.ToUpperInvariant(),
            Brand = dto.Brand,
            Model = dto.Model,
            Color = dto.Color,
            OwnerName = dto.OwnerName,
            OwnerPhone = dto.OwnerPhone
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();
        return MapToDto(vehicle);
    }

    public async Task<VehicleDto?> UpdateAsync(int id, VehicleUpdateDto dto)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle == null || vehicle.IsDeleted) return null;

        vehicle.Brand = dto.Brand ?? vehicle.Brand;
        vehicle.Model = dto.Model ?? vehicle.Model;
        vehicle.Color = dto.Color ?? vehicle.Color;
        vehicle.OwnerName = dto.OwnerName ?? vehicle.OwnerName;
        vehicle.OwnerPhone = dto.OwnerPhone ?? vehicle.OwnerPhone;
        vehicle.Notes = dto.Notes ?? vehicle.Notes;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(vehicle);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle == null) return false;

        vehicle.IsDeleted = true;
        vehicle.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(string plateNumber)
    {
        return await _context.Vehicles.AnyAsync(v => v.PlateNumber == plateNumber && !v.IsDeleted);
    }

    private static VehicleDto MapToDto(Vehicle v) => new(
        v.Id,
        v.PlateNumber,
        v.Brand,
        v.Model,
        v.Color,
        v.OwnerName);
}
