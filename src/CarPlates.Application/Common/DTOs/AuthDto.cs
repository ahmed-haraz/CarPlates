namespace CarPlates.Application.Common.DTOs;

public record LoginRequestDto(string Username, string Password);
public record LoginResponseDto(string AccessToken, string RefreshToken, UserDto User);
public record RefreshTokenRequestDto(string RefreshToken);
public record UserDto(string Username, string Email, string FullName, string? ProfilePhotoUrl, string Role);
