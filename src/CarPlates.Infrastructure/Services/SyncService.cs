using CarPlates.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Networking;

namespace CarPlates.Infrastructure.Services;

public class SyncService : ISyncService
{
    private readonly IScanRepository _scanRepository;
    private readonly IPendingUploadRepository _pendingUploadRepository;
    private readonly IVehicleLookupService _vehicleLookupService;
    private readonly ILoggingService _loggingService;
    private readonly ILogger<SyncService> _logger;

    public event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;

    public SyncService(
        IScanRepository scanRepository,
        IPendingUploadRepository pendingUploadRepository,
        IVehicleLookupService vehicleLookupService,
        ILoggingService loggingService,
        ILogger<SyncService> logger)
    {
        _scanRepository = scanRepository;
        _pendingUploadRepository = pendingUploadRepository;
        _vehicleLookupService = vehicleLookupService;
        _loggingService = loggingService;
        _logger = logger;
    }

    public async Task<SyncResult> SyncPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _pendingUploadRepository.GetPendingAsync(cancellationToken);
        var failed = await _pendingUploadRepository.GetFailedAsync(cancellationToken);
        var allPending = pending.Concat(failed).ToList();

        if (!allPending.Any())
        {
            return new SyncResult(true, 0, 0, null);
        }

        int synced = 0;
        int failedCount = 0;
        int total = allPending.Count;

        for (int i = 0; i < allPending.Count; i++)
        {
            var item = allPending[i];
            SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs(total, i + 1, $"Syncing {item.PlateNumber}"));

            try
            {
                var result = await _vehicleLookupService.LookupAsync(item.PlateNumber, cancellationToken);

                if (result.Found)
                {
                    var scanRecord = await _scanRepository.GetByIdAsync(item.ScanRecordId, cancellationToken);
                    if (scanRecord != null)
                    {
                        scanRecord.UpdateVehicleInfo(
                            result.Brand,
                            result.Model,
                            result.Color,
                            result.OwnerName,
                            result.AccessStatus);
                        await _scanRepository.UpdateAsync(scanRecord, cancellationToken);
                    }

                    item.MarkAsCompleted();
                    synced++;
                }
                else
                {
                    item.MarkAsFailed(result.ErrorMessage ?? "Vehicle not found");
                    failedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync error for {PlateNumber}", item.PlateNumber);
                item.MarkAsFailed(ex.Message);
                failedCount++;
            }

            await _pendingUploadRepository.UpdateAsync(item, cancellationToken);
        }

        _logger.LogInformation("Sync completed: {Synced} synced, {Failed} failed", synced, failedCount);
        return new SyncResult(synced > 0 || failedCount == 0, synced, failedCount, null);
    }

    public Task<bool> IsOnlineAsync()
    {
        // Check network connectivity
        var current = Connectivity.NetworkAccess;
        return Task.FromResult(current == NetworkAccess.Internet);
    }
}
