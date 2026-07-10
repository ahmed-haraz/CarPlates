using System.Text.Json;
using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Shared.Constants;
using Microsoft.Maui.Storage;

namespace CarPlates.Infrastructure.Services;

public class TokenStorage : ITokenStorage
{
    private const string CachedUserKey = "cached_current_user";

    public async Task SaveTokenAsync(string accessToken, string refreshToken)
    {
        await SecureStorage.SetAsync(AuthConstants.AccessTokenKey, accessToken);
        await SecureStorage.SetAsync(AuthConstants.RefreshTokenKey, refreshToken);
        await SecureStorage.SetAsync(AuthConstants.TokenExpiryKey, 
            DateTime.UtcNow.AddHours(1).ToString("O"));
    }

    public async Task<(string? AccessToken, string? RefreshToken)> GetTokensAsync()
    {
        var accessToken = await SecureStorage.GetAsync(AuthConstants.AccessTokenKey);
        var refreshToken = await SecureStorage.GetAsync(AuthConstants.RefreshTokenKey);
        return (accessToken, refreshToken);
    }

    public Task ClearTokensAsync()
    {
        SecureStorage.Remove(AuthConstants.AccessTokenKey);
        SecureStorage.Remove(AuthConstants.RefreshTokenKey);
        SecureStorage.Remove(AuthConstants.TokenExpiryKey);
        SecureStorage.Remove(CachedUserKey);
        return Task.CompletedTask;
    }

    public async Task<bool> HasValidTokenAsync()
    {
        var accessToken = await SecureStorage.GetAsync(AuthConstants.AccessTokenKey);
        if (string.IsNullOrEmpty(accessToken)) return false;

        var expiryString = await SecureStorage.GetAsync(AuthConstants.TokenExpiryKey);
        if (DateTime.TryParse(expiryString, out var expiry))
        {
            return expiry > DateTime.UtcNow.AddMinutes(AuthConstants.TokenRefreshMinutes);
        }

        // No parseable expiry stored alongside an access token is an unexpected/
        // corrupt state - treat as invalid rather than "valid forever" so the
        // caller is forced through a refresh (or re-login) instead of silently
        // trusting a token we can't actually verify the age of.
        return false;
    }

    public async Task SaveCurrentUserAsync(UserDto user)
    {
        var json = JsonSerializer.Serialize(user);
        await SecureStorage.SetAsync(CachedUserKey, json);
    }

    public async Task<UserDto?> GetCachedCurrentUserAsync()
    {
        var json = await SecureStorage.GetAsync(CachedUserKey);
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            return JsonSerializer.Deserialize<UserDto>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
