namespace CarPlates.Application.Common.DTOs;

public record ScanRecordDto(
    int Id,
    string PlateNumber,
    string PlateType,
    float Confidence,
    string? PhotoPath,
    DateTime ScanTime,
    string? VehicleBrandAr,
    string? VehicleBrandEn,
    string? VehicleModelAr,
    string? VehicleModelEn,
    string? VehicleColor,
    string? OwnerNameAr,
    string? OwnerNameEn,
    string? AccessStatus);

public record ScanRecordListDto(
    int Id,
    string PlateNumber,
    string PlateType,
    float Confidence,
    DateTime ScanTime,
    string? VehicleBrandAr,
    string? VehicleBrandEn,
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
    string? VehicleBrandAr,
    string? VehicleBrandEn,
    string? VehicleModelAr,
    string? VehicleModelEn,
    string? VehicleColor,
    string? OwnerNameAr,
    string? OwnerNameEn,
    string? AccessStatus);
