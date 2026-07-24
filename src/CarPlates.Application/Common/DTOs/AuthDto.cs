namespace CarPlates.Application.Common.DTOs;

public record DeviceInfoDto(string? CompanyCode, string? DeviceId, string? AppVersion, string? Manufacturer, string? Model, string? DeviceName);
public record LoginRequestDto(string Username, string Password, DeviceInfoDto? Device = null);
public record LoginResponseDto(string AccessToken, string RefreshToken, UserDto User);
public record RefreshTokenRequestDto(string RefreshToken);
public record UserDto(string Id, string Username, string Email, string FullName, int BranchId, int CashboxID, int CarId, int StoreId, int SalesRepID, int Usertype);
