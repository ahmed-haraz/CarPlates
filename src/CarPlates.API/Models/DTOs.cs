namespace CarPlates.API.Models;

// Auth DTOs
public record LoginRequestDto(string Username, string Password);
public record LoginResponseDto(string AccessToken, string RefreshToken, UserDto User);
public record RefreshTokenRequestDto(string RefreshToken);
public record LogoutRequestDto(string RefreshToken);
public record UserDto(string Id, string Username, string Email, string FullName, int BranchId, int CashboxID, int CarId, int StoreId, int SalesRepID, int Usertype);
public record RegisterRequestDto(string Username, string Email, string Password, string FullName);

// Vehicle DTOs
public record VehicleDto(
    int Id,
    string PlateNumber,
    string? Brand,
    string? Model,
    string? Color,
    string? OwnerName);

public record VehicleCreateDto(
    string PlateNumber,
    string? Brand,
    string? Model,
    string? Color,
    string? OwnerName,
    string? OwnerPhone);

public record VehicleUpdateDto(
    string? Brand,
    string? Model,
    string? Color,
    string? OwnerName,
    string? OwnerPhone,
    string? Notes);

// Scan Record DTOs
public record ScanRecordDto(
    int Id,
    string PlateNumber,
    string? PhotoUrl,
    DateTime? ScanTime,
    string? Brand,
    string? Model,
    string? Color,
    string? OwnerName);

public record ScanRecordCreateDto(
    string PlateNumber,
    string? PhotoUrl,
    string? DeviceId,
    double? Latitude,
    double? Longitude);

// Dashboard/Stats DTOs
public record DashboardStatisticsDto(
    int TotalScans,
    int TodayScans,
    int TotalVehicles);

public record RecentScanDto(
    int Id,
    string PlateNumber,
    string? VehicleBrand,
    DateTime? ScanTime);

// Sync DTOs
public record SyncBatchRequestDto(List<ScanRecordSyncDto> Records);

public record OwnerDto(

    int ID,
    string Name_ar,
    string Name_En,
    string Phone1,
    string Phone2,
    string Mobile,
    string email,
    string Address


    );


public record ScanRecordSyncDto(
    string PlateNumber,
    DateTime? ScanTime,
    string? PhotoUrl);


public record SyncBatchResponseDto(int SyncedCount, int FailedCount, List<string> Errors);
