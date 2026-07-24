using CarPlates.Application.Common.DTOs;
using MediatR;

namespace CarPlates.Application.Scanner.Commands;

public record ScanVehicleCommand(
    string PlateNumber,
    string PlateType,
    float Confidence,
    string? PhotoPath,
    double? Latitude = null,
    double? Longitude = null,
    string? Notes = null) : IRequest<ScanVehicleResult>;

public record ScanVehicleResult(
    bool Success,
    ScanRecordDto? ScanRecord,
    VehicleDetailsDto? VehicleInfo,
    string? ErrorMessage);
