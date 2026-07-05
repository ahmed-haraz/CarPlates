using CarPlates.Application.Common.Interfaces;
using CarPlates.Shared.Constants;

namespace CarPlates.Infrastructure.Api.Authentication;

public class AuthDelegatingHandler : DelegatingHandler
{
    private readonly ITokenStorage _tokenStorage;

    public AuthDelegatingHandler(ITokenStorage tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var (accessToken, _) = await _tokenStorage.GetTokensAsync();

        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
