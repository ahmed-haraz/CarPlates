namespace CarPlates.Application.Common.DTOs;

public record ScanRecordDto(
    int Id,
    string PlateNumber,
    string PlateType,
    float Confidence,
    string? PhotoPath,
    DateTime ScanTime,
    string? VehicleBrand,
    string? VehicleModel,
    string? VehicleColor,
    string? OwnerName,
    string? AccessStatus);

public record ScanRecordListDto(
    int Id,
    string PlateNumber,
    string PlateType,
    float Confidence,
    DateTime ScanTime,
    string? VehicleBrand,
    string? AccessStatus);

public record CreateScanRecordDto(
    string PlateNumber,
    string PlateType,
    float Confidence,
    string? PhotoPath,
    int BranchID = 0,
    double? Latitude = null,
    double? Longitude = null,
    string? Notes = null);

public record UpdateVehicleInfoDto(
    string? VehicleBrand,
    string? VehicleModel,
    string? VehicleColor,
    string? OwnerName,
    string? AccessStatus);
