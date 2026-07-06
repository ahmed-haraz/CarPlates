using CarPlates.API.Models.DTOs;

namespace CarPlates.API.Interface;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
    Task<LoginResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request);
    Task<bool> RegisterAsync(RegisterRequestDto request);
    Task<UserDto?> GetUserAsync(string userId);
}