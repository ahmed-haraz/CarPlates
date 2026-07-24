using CarPlates.Application.Common.DTOs;

namespace CarPlates.Application.Common.Interfaces;

public interface IDeviceRegistrationService
{
    Task<DeviceCheckResult> CheckDeviceAsync(string companyCode, string deviceId,
        string appVersion, string manufacturer, string model, string deviceName,
        CancellationToken cancellationToken = default);
}

public record DeviceCheckResult(
    bool Success,
    bool DeviceBlocked,
    bool IsNewDevice,
    string? ErrorMessage,
    FwMobileControlDto? CompanyControl = null,
    FwMobileDeviceDto? DeviceRecord = null);
