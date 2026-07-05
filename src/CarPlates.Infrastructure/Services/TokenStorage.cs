using CarPlates.Application.Common.Interfaces;
using CarPlates.Shared.Constants;
using Microsoft.Maui.Storage;

namespace CarPlates.Infrastructure.Services;

public class TokenStorage : ITokenStorage
{
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

    public async Task ClearTokensAsync()
    {
        SecureStorage.Remove(AuthConstants.AccessTokenKey);
        SecureStorage.Remove(AuthConstants.RefreshTokenKey);
        SecureStorage.Remove(AuthConstants.TokenExpiryKey);
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

        return true;
    }
}
