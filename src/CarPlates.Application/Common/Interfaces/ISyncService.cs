namespace CarPlates.Application.Common.Interfaces;

public interface ISyncService
{
    Task<SyncResult> SyncPendingAsync(CancellationToken cancellationToken = default);
    Task<bool> IsOnlineAsync();
    event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;
}

public record SyncResult(bool Success, int SyncedCount, int FailedCount, string? ErrorMessage);
public record SyncProgressEventArgs(int Total, int Current, string Status);
