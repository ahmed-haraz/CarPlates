using CarPlates.Domain.Enums;

namespace CarPlates.Domain.Entities;

public class ScanRecord : BaseEntity
{
    public string PlateNumber { get; private set; } = string.Empty;
    public string PlateType { get; private set; } = string.Empty; // Egyptian, English, etc.
    public float Confidence { get; private set; }
    public string? PhotoPath { get; private set; }
    public DateTime ScanTime { get; private set; } = DateTime.UtcNow;

    // Vehicle Info (from API)
    public string? VehicleBrand { get; private set; }
    public string? VehicleModel { get; private set; }
    public string? VehicleColor { get; private set; }
    public string? OwnerName { get; private set; }
    public string? AccessStatus { get; private set; }

    // Sync Status
    public SyncStatus SyncStatus { get; private set; } = SyncStatus.Pending;
    public int RetryCount { get; private set; }
    public string? ApiError { get; private set; }

    public ScanRecord() { } // EF Core

    public static ScanRecord Create(
        string plateNumber,
        string plateType,
        float confidence,
        string? photoPath = null)
    {
        return new ScanRecord
        {
            PlateNumber = plateNumber,
            PlateType = plateType,
            Confidence = confidence,
            PhotoPath = photoPath
        };
    }

    public void UpdateVehicleInfo(
        string? brand,
        string? model,
        string? color,
        string? ownerName,
        string? accessStatus)
    {
        VehicleBrand = brand;
        VehicleModel = model;
        VehicleColor = color;
        OwnerName = ownerName;
        AccessStatus = accessStatus;
        SyncStatus = SyncStatus.Synced;
        MarkAsUpdated();
    }

    public void MarkAsSynced()
    {
        SyncStatus = SyncStatus.Synced;
        MarkAsUpdated();
    }

    public void MarkAsFailed(string error)
    {
        SyncStatus = SyncStatus.Failed;
        ApiError = error;
        RetryCount++;
        MarkAsUpdated();
    }

    public void MarkForRetry()
    {
        if (RetryCount < 3)
        {
            SyncStatus = SyncStatus.Pending;
            MarkAsUpdated();
        }
    }
}
