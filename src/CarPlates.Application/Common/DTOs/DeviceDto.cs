namespace CarPlates.Application.Common.DTOs;

public record FwMobileControlDto(
    int Id,
    string? CoName,
    long CoCode,
    int DevicesNumber,
    int TerminalID,
    int Status,
    long InsertDateTime,
    long UpdateDateTime);

public record FwMobileDeviceDto(
    int Id,
    long CompanyCode,
    string? AppVersion,
    string? TerminalID,
    string? Manufacturer,
    string? ModelNumber,
    string? DeviceName,
    string? SerialNumber,
    string? IMEI,
    bool Online,
    bool IsBlocked,
    int Status,
    long InsertDateTime,
    long UpdateDateTime);
