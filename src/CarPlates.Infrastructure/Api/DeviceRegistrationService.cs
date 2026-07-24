using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarPlates.Infrastructure.Api;

public class DeviceRegistrationService(
    IHttpClientFactory httpClientFactory,
    ILogger<DeviceRegistrationService> logger) : IDeviceRegistrationService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private HttpClient Client => _httpClientFactory.CreateClient("DeviceApi");
    private readonly ILogger<DeviceRegistrationService> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<DeviceCheckResult> CheckDeviceAsync(
        string companyCode, string deviceId,
        string appVersion, string manufacturer, string model, string deviceName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Get company controls
            var control = await GetCompanyControlAsync(companyCode, cancellationToken);
            if (control == null)
                return new DeviceCheckResult(false, false, false, "Company not found");

            // 2. Get existing devices
            var devices = await GetDevicesAsync(companyCode, cancellationToken) ?? [];

            // 3. Check if current device already registered
            var existing = devices.FirstOrDefault(d =>
                d.TerminalID == deviceId || d.DeviceName == deviceName);

            if (existing != null)
            {
                if (existing.IsBlocked)
                    return new DeviceCheckResult(true, true, false, "Your account has been disabled. Please contact support.", control, existing);

                // Update existing device
                var updated = existing with
                {
                    Online = true,
                    AppVersion = appVersion,
                    Manufacturer = manufacturer,
                    ModelNumber = model,
                    DeviceName = deviceName,
                    UpdateDateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                await UpsertDeviceAsync(updated, cancellationToken);

                return new DeviceCheckResult(true, false, false, null, control, updated);
            }

            // 4. New device — check limit
            if (devices.Length >= control.DevicesNumber)
                return new DeviceCheckResult(true, true, false,
                    $"Device limit reached ({control.DevicesNumber}). Contact support.", control);

            // 5. Register new device
            var newDevice = new FwMobileDeviceDto(
                Id: 0,
                CompanyCode: control.CoCode,
                AppVersion: appVersion,
                TerminalID: deviceId,
                Manufacturer: manufacturer,
                ModelNumber: model,
                DeviceName: deviceName,
                SerialNumber: "",
                IMEI: "",
                Online: true,
                IsBlocked: false,
                Status: 1,
                InsertDateTime: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                UpdateDateTime: 0
            );
            var registered = await UpsertDeviceAsync(newDevice, cancellationToken);

            return new DeviceCheckResult(true, false, true, null, control, registered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Device check failed for company {CompanyCode}", companyCode);
            return new DeviceCheckResult(false, false, false, ex.Message);
        }
    }

    private async Task<FwMobileControlDto?> GetCompanyControlAsync(string companyCode, CancellationToken ct)
    {
        var response = await Client.GetAsync($"api/FwMobileControls/1/1/{companyCode}", ct);
        if (!response.IsSuccessStatusCode) return null;
        var raw = await response.Content.ReadFromJsonAsync<FwMobileControlRaw>(JsonOptions, ct);
        if (raw == null) return null;
        return new FwMobileControlDto(
            raw.Id, raw.CoName, raw.CoCode, raw.DevicesNumber,
            raw.TerminalID, raw.Status, raw.InsertDateTime, raw.UpdateDateTime);
    }

    private async Task<FwMobileDeviceDto[]?> GetDevicesAsync(string companyCode, CancellationToken ct)
    {
        var response = await Client.GetAsync($"api/FwMobileDevices/{companyCode}", ct);
        if (!response.IsSuccessStatusCode) return [];
        var raw = await response.Content.ReadFromJsonAsync<FwMobileDeviceRaw[]>(JsonOptions, ct);
        if (raw == null) return [];
        return raw.Select(r => new FwMobileDeviceDto(
            r.Id, r.CompanyCode, r.AppVersion, r.TerminalID,
            r.Manufacturer, r.ModelNumber, r.DeviceName,
            r.SerialNumber, r.IMEI, r.Online, r.IsBlocked,
            r.Status, r.InsertDateTime, r.UpdateDateTime)).ToArray();
    }

    private async Task<FwMobileDeviceDto> UpsertDeviceAsync(FwMobileDeviceDto device, CancellationToken ct)
    {
        var payload = new
        {
            device.Id,
            device.CompanyCode,
            device.AppVersion,
            device.TerminalID,
            device.Manufacturer,
            device.ModelNumber,
            device.DeviceName,
            device.SerialNumber,
            device.IMEI,
            device.Online,
            device.IsBlocked,
            device.Status,
            device.InsertDateTime,
            device.UpdateDateTime
        };

        HttpResponseMessage response;
        if (device.Id > 0)
        {
            response = await Client.PutAsJsonAsync($"api/FwMobileDevices/{device.Id}", payload, ct);
        }
        else
        {
            response = await Client.PostAsJsonAsync("api/FwMobileDevices", payload, ct);
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Device upsert failed ({Status}): {Error}", response.StatusCode, error);
            return device;
        }

        var raw = await response.Content.ReadFromJsonAsync<FwMobileDeviceRaw>(JsonOptions, ct);
        if (raw == null) return device;
        return new FwMobileDeviceDto(
            raw.Id, raw.CompanyCode, raw.AppVersion, raw.TerminalID,
            raw.Manufacturer, raw.ModelNumber, raw.DeviceName,
            raw.SerialNumber, raw.IMEI, raw.Online, raw.IsBlocked,
            raw.Status, raw.InsertDateTime, raw.UpdateDateTime);
    }

    // Raw JSON-mapped records
    private record FwMobileControlRaw(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("coName")] string? CoName,
        [property: JsonPropertyName("coCode")] long CoCode,
        [property: JsonPropertyName("devicesNumber")] int DevicesNumber,
        [property: JsonPropertyName("terminalID")] int TerminalID,
        [property: JsonPropertyName("status")] int Status,
        [property: JsonPropertyName("insertDateTime")] long InsertDateTime,
        [property: JsonPropertyName("updateDateTime")] long UpdateDateTime);

    private record FwMobileDeviceRaw(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("companyCode")] long CompanyCode,
        [property: JsonPropertyName("appVersion")] string? AppVersion,
        [property: JsonPropertyName("terminalID")] string? TerminalID,
        [property: JsonPropertyName("manufacturer")] string? Manufacturer,
        [property: JsonPropertyName("modelNumber")] string? ModelNumber,
        [property: JsonPropertyName("deviceName")] string? DeviceName,
        [property: JsonPropertyName("serialNumber")] string? SerialNumber,
        [property: JsonPropertyName("iMEI")] string? IMEI,
        [property: JsonPropertyName("online")] bool Online,
        [property: JsonPropertyName("isBlocked")] bool IsBlocked,
        [property: JsonPropertyName("status")] int Status,
        [property: JsonPropertyName("insertDateTime")] long InsertDateTime,
        [property: JsonPropertyName("updateDateTime")] long UpdateDateTime);
}
