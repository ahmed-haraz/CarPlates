using System.Net.Http.Json;
using System.Text.Json;
using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace CarPlates.Infrastructure.Api.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenStorage _tokenStorage;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IHttpClientFactory httpClientFactory,
        ITokenStorage tokenStorage,
        ILogger<AuthenticationService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("CarPlatesApi");
        _tokenStorage = tokenStorage;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting login for user: {Username}", username);

            var request = new LoginRequestDto(username, password);
            var response = await _httpClient.PostAsJsonAsync("auth/login", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Login failed for {Username}: {StatusCode}", username, response.StatusCode);
                return new AuthResult(false, null, null, $"Login failed: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken);
            if (result == null)
            {
                return new AuthResult(false, null, null, "Invalid response from server");
            }

            await _tokenStorage.SaveTokenAsync(result.AccessToken, result.RefreshToken);
            _logger.LogInformation("Login successful for {Username}", username);

            return new AuthResult(true, result.AccessToken, result.RefreshToken, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for {Username}", username);
            return new AuthResult(false, null, null, $"Network error: {ex.Message}");
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
                return new AuthResult(false, null, null, "Token refresh failed");
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken);
            if (result == null) return new AuthResult(false, null, null, "Invalid refresh response");

            await _tokenStorage.SaveTokenAsync(result.AccessToken, result.RefreshToken);
            return new AuthResult(true, result.AccessToken, result.RefreshToken, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh error");
            return new AuthResult(false, null, null, ex.Message);
        }
    }

    public async Task<bool> LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClient.PostAsync("auth/logout", null, cancellationToken);
        }
        catch { /* Ignore API errors on logout */ }
        finally
        {
            await _tokenStorage.ClearTokensAsync();
        }
        return true;
    }

    public async Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        return await _tokenStorage.HasValidTokenAsync();
    }

    public async Task<UserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("auth/me", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var user = await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken);
            if (user == null) return null;

            return new UserInfo(user.Username, user.Email, user.FullName, user.ProfilePhotoUrl, user.Role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return null;
        }
    }
}
