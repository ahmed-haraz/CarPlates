using CarPlates.Domain.Entities;
using SQLite;

namespace CarPlates.Infrastructure.Storage.Database;

public class DatabaseContext
{
    private readonly SQLiteAsyncConnection _database;

    public DatabaseContext(string dbPath)
    {
        _database = new SQLiteAsyncConnection(dbPath);
        InitializeAsync().Wait();
    }

    private async Task InitializeAsync()
    {
        await _database.CreateTableAsync<ScanRecord>();
        await _database.CreateTableAsync<PendingUpload>();
    }

    public SQLiteAsyncConnection Database => _database;
}
