namespace CarPlates.Domain.Events;

public record ScanCompletedEvent(
    Guid ScanRecordId,
    string PlateNumber,
    float Confidence,
    DateTime ScanTime);

public record VehicleLookupCompletedEvent(
    Guid ScanRecordId,
    string PlateNumber,
    bool Found,
    DateTime LookupTime);

public record SyncCompletedEvent(
    Guid ScanRecordId,
    bool Success,
    DateTime SyncTime);
