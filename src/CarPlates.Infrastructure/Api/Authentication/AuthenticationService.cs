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

}
