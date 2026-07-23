using CarPlates.API.Common;
using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

public class ScanRecordService(ApplicationDbContext context, ICustomerCarService customerCarService) : IScanRecordService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ICustomerCarService _customerCarService = customerCarService;

    public async Task<ScanRecordDto?> GetByIdAsync(int id)
    {
        var record = await _context.ScanRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        return record == null ? null : MapToDto(record);
    }

    public async Task<PagedResult<ScanRecordDto>> GetAllAsync(string? plateNumber = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20, int? branchId = null, long? userId = null)
    {
        IQueryable<ScanEvent> scanQuery = _context.ScanEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(plateNumber))
            scanQuery = scanQuery.Where(s => s.PlateNumber.Contains(plateNumber));

        if (startDate.HasValue)
            scanQuery = scanQuery.Where(s => s.ScanTime >= startDate.Value);

        if (endDate.HasValue)
            scanQuery = scanQuery.Where(s => s.ScanTime <= endDate.Value);

        if (branchId.HasValue && branchId.Value > 0)
            scanQuery = scanQuery.Where(s => s.BranchID == branchId);

        if (userId.HasValue && userId.Value > 0)
            scanQuery = scanQuery.Where(s => s.InsertUserID == userId);

        var matchingIds = await scanQuery
            .OrderByDescending(s => s.ScanTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => s.Id)
            .ToListAsync();

        var totalCount = await scanQuery.CountAsync();

        var records = await _context.ScanRecords
            .AsNoTracking()
            .Where(s => matchingIds.Contains(s.Id))
            .OrderByDescending(s => s.ScanTime)
            .ToListAsync();

        var items = records.Select(MapToDto).ToList();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PagedResult<ScanRecordDto>(items, totalCount, page, pageSize, totalPages);
    }

    public async Task<IReadOnlyList<RecentScanDto>> GetRecentAsync(int count = 10, int? branchId = null, long? userId = null)
    {
        var query = _context.ScanEvents.AsNoTracking().AsQueryable();

        if (branchId.HasValue && branchId.Value > 0)
            query = query.Where(s => s.BranchID == branchId);

        if (userId.HasValue && userId.Value > 0)
            query = query.Where(s => s.InsertUserID == userId);

        var records = await query
            .OrderByDescending(s => s.ScanTime)
            .Take(count)
            .ToListAsync();

        return [.. records.Select(r => new RecentScanDto(
            r.Id,
            r.PlateNumber,
            null,
            r.ScanTime))];
    }

    public async Task<ScanRecordDto> CreateAsync(ScanRecordCreateDto dto, string? userId = null)
    {
        var plateNumber = dto.PlateNumber.ToUpperInvariant();
        var userIdLong = long.TryParse(userId, out var uid) ? (long?)uid : null;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Plain lookup only - if the plate is already registered in wh_CustomerCars, link
        // the scan to it. If it isn't, CustomerCarID is left null: scanning an unregistered
        // plate never creates a wh_Customers/wh_CustomersBranch/wh_CustomerCars row anymore,
        // it only ever writes the wh_ScanRecords row below.
        long? customerCarId = null;

        var existingCar = await _customerCarService.GetByPlateAsync(plateNumber);
        if (existingCar != null)
        {
            customerCarId = existingCar.Id;
        }

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

    public async Task<DashboardStatisticsDto> GetStatisticsAsync(int? branchId = null, long? userId = null)
    {
        var today = DateTime.UtcNow.Date;

        var scanQuery = _context.ScanEvents.AsNoTracking().Where(s => s.Status == 1);
        if (branchId.HasValue && branchId.Value > 0)
            scanQuery = scanQuery.Where(s => s.BranchID == branchId);
        if (userId.HasValue && userId.Value > 0)
            scanQuery = scanQuery.Where(s => s.InsertUserID == userId);

        var totalScans = await scanQuery.CountAsync();
        var todayScans = await scanQuery.CountAsync(s => s.ScanTime >= today);

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
                    null, null, null,100);

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
