using CarPlates.API.Models;

namespace CarPlates.API.Interface;

public interface IAuthService
{
    Task<LoginServiceResult?> LoginAsync(LoginRequestDto request);
    Task<LoginResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request);
    Task<bool> RegisterAsync(RegisterRequestDto request);
    Task<UserDto?> GetUserAsync(string userId);
    Task<bool> LogoutAsync(string refreshToken);
}

public record LoginServiceResult(LoginResponseDto? Data, string? ErrorMessage, bool DeviceBlocked)
{
    public bool IsSuccess => ErrorMessage == null && Data != null;
    public static LoginServiceResult Success(LoginResponseDto data) => new(data, null, false);
    public static LoginServiceResult DeviceError(string message, bool blocked) => new(null, message, blocked);
}