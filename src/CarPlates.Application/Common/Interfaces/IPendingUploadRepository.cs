using CarPlates.Domain.Entities;

namespace CarPlates.Application.Common.Interfaces;

public interface IPendingUploadRepository
{
    Task<IReadOnlyList<PendingUpload>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendingUpload>> GetFailedAsync(CancellationToken cancellationToken = default);
    Task AddAsync(PendingUpload pendingUpload, CancellationToken cancellationToken = default);
    Task UpdateAsync(PendingUpload pendingUpload, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}
