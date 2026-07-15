using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

public class ScanRecordService(ApplicationDbContext context) : IScanRecordService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ScanRecordDto?> GetByIdAsync(int id)
    {
        var record = await _context.ScanRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        return record == null ? null : MapToDto(record);
    }

    public async Task<IReadOnlyList<ScanRecordDto>> GetAllAsync(string? plateNumber = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        IQueryable<ScanRecord> query = _context.ScanRecords.AsNoTracking();

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
            .OrderByDescending(s => s.ScanTime)
            .Take(count)
            .ToListAsync();

        return [.. records.Select(r => new RecentScanDto(
            r.Id,
            r.PlateNumber,
            r.Brand,
            r.ScanTime))];
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
                
            };
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
        }

        var record = new ScanRecord
        {
            PlateNumber = dto.PlateNumber.ToUpperInvariant(),
            PhotoUrl = dto.PhotoUrl,
            ScannedByUserId = userId,
            DeviceId = dto.DeviceId,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            Color = vehicle.Color,
            OwnerName = vehicle.OwnerName
        };

        _context.ScanRecords.Add(record);
        await _context.SaveChangesAsync();

        // Reload with vehicle
        return MapToDto(record);
    }

    public async Task<DashboardStatisticsDto> GetStatisticsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var totalScans = await _context.ScanRecords.CountAsync();
        var todayScans = await _context.ScanRecords.CountAsync(s => s.ScanTime >= today);
        var totalVehicles = await _context.Vehicles.CountAsync(v => !v.IsDeleted);
        

        return new DashboardStatisticsDto(totalScans, todayScans, totalVehicles);
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
        s.PhotoUrl,
        s.ScanTime,
        s.Brand,
        s.Model,
        s.Color,
        s.OwnerName);
}
