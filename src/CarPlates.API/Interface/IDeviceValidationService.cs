namespace CarPlates.API.Interface;

public interface IDeviceValidationService
{
    Task<DeviceValidationResult> ValidateDeviceAsync(string companyCode, string deviceId,
        string appVersion, string manufacturer, string model, string deviceName);
}

public record DeviceValidationResult(bool IsValid, bool IsBlocked, bool LimitExceeded, string? ErrorMessage);
