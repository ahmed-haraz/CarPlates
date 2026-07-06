namespace CarPlates.API.Models.DTOs;

// Auth DTOs
public record LoginRequestDto(string Username, string Password);
public record LoginResponseDto(string AccessToken, string RefreshToken, UserDto User);
public record RefreshTokenRequestDto(string RefreshToken);
public record UserDto(string Id, string Username, string Email, string FullName, string? ProfilePhotoUrl, string Role);
public record RegisterRequestDto(string Username, string Email, string Password, string FullName);

// Vehicle DTOs
public record VehicleDto(
    Guid Id,
    string PlateNumber,
    string PlateType,
    string? Brand,
    string? Model,
    string? Color,
    string? OwnerName,
    string AccessStatus);

public record VehicleCreateDto(
    string PlateNumber,
    string PlateType,
    string? Brand,
    string? Model,
    string? Color,
    string? OwnerName,
    string? OwnerPhone,
    string? OwnerNationalId,
    string AccessStatus);

public record VehicleUpdateDto(
    string? Brand,
    string? Model,
    string? Color,
    string? OwnerName,
    string? OwnerPhone,
    string? AccessStatus,
    string? Notes);

// Scan Record DTOs
public record ScanRecordDto(
    Guid Id,
    string PlateNumber,
    float Confidence,
    DateTime ScanTime,
    VehicleDto? Vehicle);

public record ScanRecordCreateDto(
    string PlateNumber,
    string PlateType,
    float Confidence,
    string? PhotoUrl,
    string? DeviceId,
    double? Latitude,
    double? Longitude);

// Dashboard/Stats DTOs
public record DashboardStatisticsDto(
    int TotalScans,
    int TodayScans,
    int TotalVehicles,
    int AllowedVehicles,
    int DeniedVehicles,
    int PendingVehicles);

public record RecentScanDto(
    Guid Id,
    string PlateNumber,
    string? VehicleBrand,
    string AccessStatus,
    DateTime ScanTime);

// Sync DTOs
public record SyncBatchRequestDto(List<ScanRecordSyncDto> Records);
public record ScanRecordSyncDto(
    string PlateNumber,
    string PlateType,
    float Confidence,
    DateTime ScanTime,
    string? PhotoUrl);
public record SyncBatchResponseDto(int SyncedCount, int FailedCount, List<string> Errors);
