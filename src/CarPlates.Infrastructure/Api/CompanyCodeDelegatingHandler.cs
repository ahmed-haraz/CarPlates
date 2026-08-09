using CarPlates.Shared.Constants;
using Microsoft.Maui.Storage;

namespace CarPlates.Infrastructure.Api;

/// <summary>
/// Adds the X-Company-Code request header to every API call so the server can resolve
/// the correct database for the company. Reads the currently configured company code
/// from Preferences on every request, so changing it in Settings takes effect on the
/// very next call without restarting the app.
/// </summary>
public class CompanyCodeDelegatingHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var companyCode = Preferences.Get(CompanyConstants.CompanyCodePreference, AuthConstants.DefaultCompanyCode);

        if (!string.IsNullOrWhiteSpace(companyCode) &&
            !request.Headers.Contains(CompanyConstants.CompanyCodeHeader))
        {
            request.Headers.TryAddWithoutValidation(CompanyConstants.CompanyCodeHeader, companyCode);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
