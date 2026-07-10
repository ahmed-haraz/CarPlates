using CarPlates.Application.Common.DTOs;

namespace CarPlates.Application.Common.Interfaces;

public interface ITokenStorage
{
    Task SaveTokenAsync(string accessToken, string refreshToken);
    Task<(string? AccessToken, string? RefreshToken)> GetTokensAsync();
    Task ClearTokensAsync();
    Task<bool> HasValidTokenAsync();

    // Caches the logged-in user's profile locally so pages like Dashboard/Profile
    // can display it instantly without a network round trip, and so it survives
    // app restarts alongside the token.
    Task SaveCurrentUserAsync(UserDto user);
    Task<UserDto?> GetCachedCurrentUserAsync();
}
