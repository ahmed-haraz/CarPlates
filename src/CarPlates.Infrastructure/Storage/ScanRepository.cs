using CarPlates.Application.Common.Interfaces;
using CarPlates.Domain.Entities;
using CarPlates.Infrastructure.Storage.Database;
using SQLite;

namespace CarPlates.Infrastructure.Storage;

public class ScanRepository : IScanRepository
{
    private readonly DatabaseContext _context;
    private readonly SQLiteAsyncConnection _database;

    public ScanRepository(DatabaseContext context)
    {
        _context = context;
        _database = context.Database;
    }

    public async Task<ScanRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _context.EnsureInitializedAsync(cancellationToken);

        return await _database.FindAsync<ScanRecord>(id);
    }

    public async Task<IReadOnlyList<ScanRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _context.EnsureInitializedAsync(cancellationToken);

        var records = await _database.Table<ScanRecord>()
            .Where(r => r.IsDeleted == false)
            .OrderByDescending(r => r.ScanTime)
            .ToListAsync();
        return records.AsReadOnly();
    }

    public async Task<IReadOnlyList<ScanRecord>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        await _context.EnsureInitializedAsync(cancellationToken);

        var records = await _database.Table<ScanRecord>()
            .Where(r => r.IsDeleted == false)
            .OrderByDescending(r => r.ScanTime)
            .Take(count)
            .ToListAsync();
        return records.AsReadOnly();
    }

    public async Task<IReadOnlyList<ScanRecord>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        await _context.EnsureInitializedAsync(cancellationToken);

        var records = await _database.Table<ScanRecord>()
            .Where(r => r.ScanTime >= start && r.ScanTime <= end && r.IsDeleted == false)
            .OrderByDescending(r => r.ScanTime)
            .ToListAsync();
        return records.AsReadOnly();
    }

    public async Task<IReadOnlyList<ScanRecord>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        await _context.EnsureInitializedAsync(cancellationToken);

        var records = await _database.Table<ScanRecord>()
            .Where(r => r.PlateNumber.Contains(query) && r.IsDeleted == false)
            .OrderByDescending(r => r.ScanTime)
            .ToListAsync();
        return records.AsReadOnly();
    }

    public async Task<IReadOnlyList<ScanRecord>> GetPendingSyncAsync(CancellationToken cancellationToken = default)
    {
        await _context.EnsureInitializedAsync(cancellationToken);

        var records = await _database.Table<ScanRecord>()
            .Where(r => r.SyncStatus == Domain.Enums.SyncStatus.Pending && r.IsDeleted == false)
            .ToListAsync();
        return records.AsReadOnly();
    }

    public async Task<ScanRecord?> GetByPlateNumberAsync(string plateNumber, CancellationToken cancellationToken = default)
    {
        await _context.EnsureInitializedAsync(cancellationToken);

        return await _database.Table<ScanRecord>()
            .Where(r => r.PlateNumber == plateNumber && r.IsDeleted == false)
            .OrderByDescending(r => r.ScanTime)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<ScanRecord>> GetAllByPlateNumberAsync(string plateNumber, CancellationToken cancellationToken = default)
    {
        await _context.EnsureInitializedAsync(cancellationToken);

        var records = await _database.Table<ScanRecord>()
            .Where(r => r.PlateNumber == plateNumber && r.IsDeleted == false)
            .OrderByDescending(r => r.ScanTime)
            .ToListAsync();
        return records.AsReadOnly();
    }

    public async Task AddAsync(ScanRecord scanRecord, CancellationToken cancellationToken = default)
    {
        await _context.EnsureInitializedAsync(cancellationToken);

        await _database.InsertAsync(scanRecord);
    }

    public async Task UpdateAsync(ScanRecord scanRecord, CancellationToken cancellationToken = default)
    {
        await _context.EnsureInitializedAsync(cancellationToken);

        await _database.UpdateAsync(scanRecord);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _context.EnsureInitializedAsync(cancellationToken);

        var record = await GetByIdAsync(id, cancellationToken);
        if (record != null)
        {
            record.MarkAsDeleted();
            await UpdateAsync(record, cancellationToken);
        }
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        await _context.EnsureInitializedAsync(cancellationToken);

        return await _database.Table<ScanRecord>()
            .Where(r => r.IsDeleted == false)
            .CountAsync();
    }

    public async Task<int> GetTodayCountAsync(CancellationToken cancellationToken = default)
    {
        await _context.EnsureInitializedAsync(cancellationToken);

        var today = DateTime.UtcNow.Date;
        return await _database.Table<ScanRecord>()
            .Where(r => r.ScanTime >= today && r.IsDeleted == false)
            .CountAsync();
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        await _context.EnsureInitializedAsync(cancellationToken);

        await _database.DeleteAllAsync<ScanRecord>();
    }
}
