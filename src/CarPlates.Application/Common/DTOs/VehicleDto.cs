namespace CarPlates.Application.Common.DTOs;

public record VehicleDetailsDto(
    string PlateNumber,
    string? Brand,
    string? Model,
    string? Color,
    string? OwnerName,
    string? AccessStatus,
    DateTime? LastScanTime,
    int TotalScans,
    string? PhotoUrl,
    string? PlateType = null,
    long? CarHeaderId = null,
    string? CustomerName_Ar = null,
    string? CustomerName_En = null,
    string? CustomerMobile = null);
