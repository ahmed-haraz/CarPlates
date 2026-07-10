using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace CarPlates.Infrastructure.Api.Authentication;

public class AuthenticationService(
    IHttpClientFactory httpClientFactory,
    ITokenStorage tokenStorage,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("CarPlatesApi");
    private readonly ITokenStorage _tokenStorage = tokenStorage;
    private readonly ILogger<AuthenticationService> _logger = logger;

    public async Task<AuthResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting login for user: {Username}", username);

            var request = new LoginRequestDto(username, password);
            var response = await _httpClient.PostAsJsonAsync("Auth/login", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Login failed for {Username}: {StatusCode}", username, response.StatusCode);
                return new AuthResult(false, null, null, $"Login failed: {error}", null!);
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken);
            if (result == null)
            {
                return new AuthResult(false, null, null, "Invalid response from server", null!);
            }

            await _tokenStorage.SaveTokenAsync(result.AccessToken, result.RefreshToken);
            await _tokenStorage.SaveCurrentUserAsync(result.User);
            _logger.LogInformation("Login successful for {Username}", username);

            return new AuthResult(true, result.AccessToken, result.RefreshToken, null, result.User);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for {Username}", username);
            return new AuthResult(false, null, null, $"Network error: {ex.Message}", null!);
        }
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new RefreshTokenRequestDto(refreshToken);
            var response = await _httpClient.PostAsJsonAsync("auth/refresh", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await _tokenStorage.ClearTokensAsync();
                return new AuthResult(false, null, null, "Token refresh failed", null!);
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken);
            if (result == null) return new AuthResult(false, null, null, "Invalid refresh response", null!);

            await _tokenStorage.SaveTokenAsync(result.AccessToken, result.RefreshToken);
            await _tokenStorage.SaveCurrentUserAsync(result.User);
            return new AuthResult(true, result.AccessToken, result.RefreshToken, null, result.User);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh error");
            return new AuthResult(false, null, null, ex.Message, null!);
        }
    }

    public async Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        return await _tokenStorage.HasValidTokenAsync();
    }

    public async Task<UserDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: serve the profile cached at login time so Dashboard/Profile
        // can render instantly without a network round trip.
        var cached = await _tokenStorage.GetCachedCurrentUserAsync();
        if (cached != null)
        {
            return cached;
        }

        // Fallback: cache missed (e.g. cleared, or app updated) but we still hold
        // a valid token - ask the API who we are and re-cache the result.
        if (!await _tokenStorage.HasValidTokenAsync())
        {
            return null;
        }

        try
        {
            var response = await _httpClient.GetAsync("Auth/me", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var user = await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken);
            if (user != null)
            {
                await _tokenStorage.SaveCurrentUserAsync(user);
            }
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch current user");
            return null;
        }
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var (_, refreshToken) = await _tokenStorage.GetTokensAsync();
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _httpClient.PostAsJsonAsync(
                    "Auth/logout",
                    new RefreshTokenRequestDto(refreshToken),
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Even if revoking the token server-side fails (offline, server down,
            // etc.) we must still clear the local session below - a failed
            // network call must never trap the user in a "logged in" state.
            _logger.LogWarning(ex, "Server-side logout call failed; clearing local session anyway");
        }
        finally
        {
            await _tokenStorage.ClearTokensAsync();
        }
    }
}
