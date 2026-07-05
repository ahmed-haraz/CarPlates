using CarPlates.Application.Common.Interfaces;
using CarPlates.Domain.Entities;
using CarPlates.Infrastructure.Storage.Database;
using SQLite;

namespace CarPlates.Infrastructure.Storage;

public class PendingUploadRepository(DatabaseContext context) : IPendingUploadRepository
{
    private readonly SQLiteAsyncConnection _database = context.Database;

    public async Task<IReadOnlyList<PendingUpload>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var records = await _database.Table<PendingUpload>()
            .Where(p => p.Status == Domain.Enums.UploadStatus.Pending)
            .ToListAsync();
        return records.AsReadOnly();
    }

    public async Task<IReadOnlyList<PendingUpload>> GetFailedAsync(CancellationToken cancellationToken = default)
    {
        var records = await _database.Table<PendingUpload>()
            .Where(p => p.Status == Domain.Enums.UploadStatus.Failed && p.RetryCount < 3)
            .ToListAsync();
        return records.AsReadOnly();
    }

    public async Task AddAsync(PendingUpload pendingUpload, CancellationToken cancellationToken = default)
    {
        await _database.InsertAsync(pendingUpload);
    }

    public async Task UpdateAsync(PendingUpload pendingUpload, CancellationToken cancellationToken = default)
    {
        await _database.UpdateAsync(pendingUpload);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _database.DeleteAsync<PendingUpload>(id);
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await _database.Table<PendingUpload>()
            .Where(p => p.Status == Domain.Enums.UploadStatus.Pending)
            .CountAsync();
    }
}
