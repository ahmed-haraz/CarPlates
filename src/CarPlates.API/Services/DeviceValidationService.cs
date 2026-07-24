using CarPlates.API.Interface;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarPlates.API.Services;

public class DeviceValidationService(IHttpClientFactory httpClientFactory, ILogger<DeviceValidationService> logger) : IDeviceValidationService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<DeviceValidationService> _logger = logger;

    private HttpClient Client => _httpClientFactory.CreateClient("FwApi");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<DeviceValidationResult> ValidateDeviceAsync(
        string companyCode, string deviceId,
        string appVersion, string manufacturer, string model, string deviceName)
    {
        try
        {
            var control = await GetCompanyControlAsync(companyCode);
            if (control == null)
                return new DeviceValidationResult(false, false, false, "Company not found");

            var devices = await GetDevicesAsync(companyCode) ?? [];

            var existing = devices.FirstOrDefault(d =>
                d.TerminalID == deviceId || d.DeviceName == deviceName);

            if (existing != null)
            {
                if (existing.IsBlocked)
                    return new DeviceValidationResult(true, true, false, "This device has been disabled. Please contact support.");

                await UpsertDeviceAsync(existing with
                {
                    Online = true,
                    AppVersion = appVersion,
                    Manufacturer = manufacturer,
                    ModelNumber = model,
                    DeviceName = deviceName,
                    UpdateDateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });

                return new DeviceValidationResult(true, false, false, null);
            }

            if (devices.Length >= control.DevicesNumber)
                return new DeviceValidationResult(true, false, true, $"Device limit reached ({control.DevicesNumber}). Contact support.");

            var newDevice = new FwMobileDeviceRaw(
                0,
                control.CoCode,
                appVersion,
                deviceId,
                manufacturer,
                model,
                deviceName,
                "",    // SerialNumber
                "",    // IMEI
                true,  // Online
                false, // IsBlocked
                1,     // Status
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                0      // UpdateDateTime
            );

            await UpsertDeviceAsync(newDevice);
            return new DeviceValidationResult(true, false, false, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Device validation failed for company {CompanyCode}", companyCode);
            return new DeviceValidationResult(false, false, false, ex.Message);
        }
    }

    private async Task<FwMobileControlRaw?> GetCompanyControlAsync(string companyCode)
    {
        var response = await Client.GetAsync($"api/FwMobileControls/1/1/{companyCode}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<FwMobileControlRaw>(JsonOptions);
    }

    private async Task<FwMobileDeviceRaw[]?> GetDevicesAsync(string companyCode)
    {
        var response = await Client.GetAsync($"api/FwMobileDevices/{companyCode}");
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<FwMobileDeviceRaw[]>(JsonOptions);
    }

    private async Task UpsertDeviceAsync(FwMobileDeviceRaw device)
    {
        HttpResponseMessage response;
        if (device.Id > 0)
        {
            response = await Client.PutAsJsonAsync($"api/FwMobileDevices/{device.Id}", device, JsonOptions);
        }
        else
        {
            response = await Client.PostAsJsonAsync("api/FwMobileDevices", device, JsonOptions);
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Device upsert failed ({Status}): {Error}", response.StatusCode, error);
        }
    }

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
