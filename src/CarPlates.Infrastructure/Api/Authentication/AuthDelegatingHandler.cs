using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;

namespace CarPlates.Infrastructure.Api.Authentication;

public class AuthDelegatingHandler(ITokenStorage tokenStorage, string apiBaseUrl) : DelegatingHandler
{
    private readonly ITokenStorage _tokenStorage = tokenStorage;

    // The API base (e.g. "http://host:port/api/v1/") that "CarPlatesApi" HttpClient
    // was configured with. Needed to build the refresh-token request URL
    // correctly: resolving "auth/refresh" relative to the *current* request's
    // URI (e.g. ".../api/v1/vehicles/ABC123") would replace the last path
    // segment and produce ".../api/v1/auth/refresh" only by accident for
    // single-segment paths, and silently break for anything nested deeper.
    private readonly Uri _apiBaseUri = new(apiBaseUrl.EndsWith('/') ? apiBaseUrl : apiBaseUrl + "/");

    // Guards against concurrent requests each independently kicking off a
    // refresh when a token expires; only one refresh call happens at a time
    // and the rest wait for and reuse its result.
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var (accessToken, _) = await _tokenStorage.GetTokensAsync();

        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        // Access token was rejected - try exactly one silent refresh-and-retry
        // before giving up, so a mid-session expiry doesn't look like a random
        // API failure to the rest of the app.
        var (_, refreshToken) = await _tokenStorage.GetTokensAsync();
        if (string.IsNullOrEmpty(refreshToken))
        {
            return response;
        }

        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            // Another request may have already refreshed while we waited for the lock.
            var (currentAccessToken, currentRefreshToken) = await _tokenStorage.GetTokensAsync();
            if (!string.IsNullOrEmpty(currentAccessToken) && currentAccessToken != accessToken)
            {
                response.Dispose();
                return await RetryWithTokenAsync(request, currentAccessToken, cancellationToken);
            }

            if (string.IsNullOrEmpty(currentRefreshToken))
            {
                return response;
            }

            // Refresh endpoint lives under the same API base as everything else;
            // resolved relative to the original request rather than hardcoded so
            // this keeps working if the configured API URL changes at runtime.
            var refreshResponse = await base.SendAsync(
                BuildRefreshRequest(currentRefreshToken),
                cancellationToken);

            if (!refreshResponse.IsSuccessStatusCode)
            {
                await _tokenStorage.ClearTokensAsync();
                return response;
            }

            var result = await refreshResponse.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken);
            if (result == null)
            {
                await _tokenStorage.ClearTokensAsync();
                return response;
            }

            await _tokenStorage.SaveTokenAsync(result.AccessToken, result.RefreshToken);
            response.Dispose();
            return await RetryWithTokenAsync(request, result.AccessToken, cancellationToken);
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private HttpRequestMessage BuildRefreshRequest(string refreshToken)
    {
        var refreshUri = new Uri(_apiBaseUri, "auth/refresh");
        return new HttpRequestMessage(HttpMethod.Post, refreshUri)
        {
            Content = JsonContent.Create(new RefreshTokenRequestDto(refreshToken))
        };
    }

    private async Task<HttpResponseMessage> RetryWithTokenAsync(HttpRequestMessage originalRequest, string accessToken, CancellationToken cancellationToken)
    {
        var retryRequest = await CloneRequestAsync(originalRequest);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        if (original.Content != null)
        {
            var buffer = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(buffer);
            foreach (var header in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        foreach (var header in original.Headers)
        {
            if (header.Key == "Authorization") continue;
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
