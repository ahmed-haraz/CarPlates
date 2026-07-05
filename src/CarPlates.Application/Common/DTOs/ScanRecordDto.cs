namespace CarPlates.Application.Common.DTOs;

public record ScanRecordDto(
    Guid Id,
    string PlateNumber,
    string PlateType,
    float Confidence,
    string? PhotoPath,
    DateTime ScanTime,
    string? VehicleBrand,
    string? VehicleModel,
    string? VehicleColor,
    string? OwnerName,
    string? AccessStatus,
    string SyncStatus,
    int RetryCount);

public record ScanRecordListDto(
    Guid Id,
    string PlateNumber,
    string PlateType,
    float Confidence,
    DateTime ScanTime,
    string? VehicleBrand,
    string? AccessStatus,
    string SyncStatus);

public record CreateScanRecordDto(
    string PlateNumber,
    string PlateType,
    float Confidence,
    string? PhotoPath);

public record UpdateVehicleInfoDto(
    string? VehicleBrand,
    string? VehicleModel,
    string? VehicleColor,
    string? OwnerName,
    string? AccessStatus);
