using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using CarPlates.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

public class ScanRecordService(ApplicationDbContext context, IVehicleService vehicleService) : IScanRecordService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IVehicleService _vehicleService = vehicleService;

    public async Task<ScanRecordDto?> GetByIdAsync(Guid id)
    {
        var record = await _context.ScanRecords
            .AsNoTracking()
            .Include(s => s.Vehicle)
            .FirstOrDefaultAsync(s => s.Id == id);

        return record == null ? null : MapToDto(record);
    }

    public async Task<IReadOnlyList<ScanRecordDto>> GetAllAsync(string? plateNumber = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        IQueryable<ScanRecord> query = _context.ScanRecords.AsNoTracking().Include(s => s.Vehicle);

        if (!string.IsNullOrWhiteSpace(plateNumber))
            query = query.Where(s => s.PlateNumber.Contains(plateNumber));

        if (startDate.HasValue)
            query = query.Where(s => s.ScanTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.ScanTime <= endDate.Value);

        var records = await query.OrderByDescending(s => s.ScanTime).ToListAsync();
        return records.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<RecentScanDto>> GetRecentAsync(int count = 10)
    {
        var records = await _context.ScanRecords
            .AsNoTracking()
            .Include(s => s.Vehicle)
            .OrderByDescending(s => s.ScanTime)
            .Take(count)
            .ToListAsync();

        return records.Select(r => new RecentScanDto(
            r.Id,
            r.PlateNumber,
            r.Vehicle?.Brand,
            r.Vehicle?.AccessStatus ?? "Unknown",
            r.ScanTime)).ToList();
    }

    public async Task<ScanRecordDto> CreateAsync(ScanRecordCreateDto dto, string? userId = null)
    {
        // Find or create vehicle
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.PlateNumber == dto.PlateNumber && !v.IsDeleted);

        if (vehicle == null)
        {
            vehicle = new Vehicle
            {
                PlateNumber = dto.PlateNumber.ToUpperInvariant(),
                PlateType = dto.PlateType,
                AccessStatus = "Pending"
            };
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
        }

        var record = new ScanRecord
        {
            VehicleId = vehicle.Id,
            PlateNumber = dto.PlateNumber.ToUpperInvariant(),
            Confidence = dto.Confidence,
            PhotoUrl = dto.PhotoUrl,
            ScannedByUserId = userId,
            DeviceId = dto.DeviceId,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };

        _context.ScanRecords.Add(record);
        await _context.SaveChangesAsync();

        // Reload with vehicle
        record.Vehicle = vehicle;
        return MapToDto(record);
    }

    public async Task<DashboardStatisticsDto> GetStatisticsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var totalScans = await _context.ScanRecords.CountAsync();
        var todayScans = await _context.ScanRecords.CountAsync(s => s.ScanTime >= today);
        var totalVehicles = await _context.Vehicles.CountAsync(v => !v.IsDeleted);
        var allowed = await _context.Vehicles.CountAsync(v => v.AccessStatus == "Allowed" && !v.IsDeleted);
        var denied = await _context.Vehicles.CountAsync(v => v.AccessStatus == "Denied" && !v.IsDeleted);
        var pending = await _context.Vehicles.CountAsync(v => v.AccessStatus == "Pending" && !v.IsDeleted);

        return new DashboardStatisticsDto(totalScans, todayScans, totalVehicles, allowed, denied, pending);
    }

    public async Task<SyncBatchResponseDto> SyncBatchAsync(SyncBatchRequestDto dto, string? userId = null)
    {
        var errors = new List<string>();
        int synced = 0;

        foreach (var recordDto in dto.Records)
        {
            try
            {
                var createDto = new ScanRecordCreateDto(
                    recordDto.PlateNumber,
                    recordDto.PlateType,
                    recordDto.Confidence,
                    recordDto.PhotoUrl,
                    null, null, null);

                await CreateAsync(createDto, userId);
                synced++;
            }
            catch (Exception ex)
            {
                errors.Add($"{recordDto.PlateNumber}: {ex.Message}");
            }
        }

        return new SyncBatchResponseDto(synced, errors.Count, errors);
    }

    private static ScanRecordDto MapToDto(ScanRecord s) => new(
        s.Id,
        s.PlateNumber,
        s.Confidence,
        s.ScanTime,
        s.Vehicle == null ? null : new VehicleDto(
            s.Vehicle.Id,
            s.Vehicle.PlateNumber,
            s.Vehicle.PlateType,
            s.Vehicle.Brand,
            s.Vehicle.Model,
            s.Vehicle.Color,
            s.Vehicle.OwnerName,
            s.Vehicle.AccessStatus));
}
