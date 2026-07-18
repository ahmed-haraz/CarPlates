using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

public class ScanRecordService(ApplicationDbContext context, ICustomerCarService customerCarService) : IScanRecordService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ICustomerCarService _customerCarService = customerCarService;

    public async Task<ScanRecordDto?> GetByIdAsync(long id)
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
        var plateNumber = dto.PlateNumber.ToUpperInvariant();
        var userIdLong = long.TryParse(userId, out var uid) ? (long?)uid : null;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Same lookup/registration path as POST customercars/scan, so a plate scanned via
        // /scans is registered the exact same way (find-or-create customer/branch/car).
        long? customerCarId = null;

        var existingCar = await _customerCarService.GetByPlateAsync(plateNumber);
        if (existingCar != null)
        {
            customerCarId = existingCar.Id;
        }
        else if (dto.BranchID.HasValue)
        {
            var scanDto = new CustomerCarScanDto(
                plateNumber,
                dto.BranchID.Value,
                dto.VIN,
                dto.Color,
                dto.VehicleYear,
                dto.CarMakesID,
                dto.CarModelID,
                dto.VehicleType,
                dto.EngineType,
                dto.CustomerName_Ar,
                dto.CustomerName_En,
                dto.CustomerMobile,
                dto.CustomerPhone1);

            var registerResult = await _customerCarService.ScanOrRegisterAsync(scanDto, userId);
            customerCarId = registerResult.Car.Id;
        }
        // else: no BranchID supplied, so there's not enough to register a new car - the
        // scan still gets logged, just with CustomerCarID left null (unregistered plate).

        var scanEvent = new ScanEvent
        {
            PlateNumber = plateNumber,
            CustomerCarID = customerCarId,
            PhotoUrl = dto.PhotoUrl,
            DeviceId = dto.DeviceId,
            BranchID = dto.BranchID,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            ScanTime = DateTime.UtcNow,
            InsertUserID = userIdLong,
            InsertDateTime = now,
        };

        _context.ScanEvents.Add(scanEvent);
        await _context.SaveChangesAsync();

        var record = await _context.ScanRecords.AsNoTracking().FirstAsync(s => s.Id == scanEvent.Id);
        return MapToDto(record);
    }

    public async Task<DashboardStatisticsDto> GetStatisticsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var totalScans = await _context.ScanEvents.CountAsync(s => s.Status == 1);
        var todayScans = await _context.ScanEvents.CountAsync(s => s.Status == 1 && s.ScanTime >= today);
        var totalVehicles = await _context.CustomerCarsFull.CountAsync(c => c.CarStatus != 0);
        var totalRegisteredCars = await _context.CustomerCars.CountAsync(c => c.Status == 1);
        var totalCustomers = await _context.WhCustomers.CountAsync(c => !c.Inactive);

        return new DashboardStatisticsDto(totalScans, todayScans, totalVehicles, totalRegisteredCars, totalCustomers);
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
