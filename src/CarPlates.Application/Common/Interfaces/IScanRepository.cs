using CarPlates.Domain.Entities;

namespace CarPlates.Application.Common.Interfaces;

public interface IScanRepository
{
    Task<ScanRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScanRecord>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScanRecord>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScanRecord>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScanRecord>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScanRecord>> GetPendingSyncAsync(CancellationToken cancellationToken = default);
    Task<ScanRecord?> GetByPlateNumberAsync(string plateNumber, CancellationToken cancellationToken = default);
    Task AddAsync(ScanRecord scanRecord, CancellationToken cancellationToken = default);
    Task UpdateAsync(ScanRecord scanRecord, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetTodayCountAsync(CancellationToken cancellationToken = default);
}
