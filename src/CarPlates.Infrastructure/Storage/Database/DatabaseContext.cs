using CarPlates.Domain.Entities;
using SQLite;

namespace CarPlates.Infrastructure.Storage.Database;

public class DatabaseContext
{
    private readonly SQLiteAsyncConnection _database;
    private readonly SemaphoreSlim _initializeLock = new(1, 1);
    private bool _isInitialized;

    public DatabaseContext(string dbPath)
    {
        _database = new SQLiteAsyncConnection(dbPath);
    }

    public SQLiteAsyncConnection Database => _database;

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        await _initializeLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
            {
                return;
            }

            await _database.CreateTableAsync<ScanRecord>();
            await _database.CreateTableAsync<PendingUpload>();
            _isInitialized = true;
        }
        finally
        {
            _initializeLock.Release();
        }
    }
}
