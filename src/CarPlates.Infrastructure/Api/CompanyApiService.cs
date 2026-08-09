using CarPlates.Application.Common.Interfaces;
using CarPlates.Shared.Constants;
using System.Diagnostics;
using System.Net.Http.Json;

namespace CarPlates.Infrastructure.Api;

/// <summary>
/// Fetches company display info (name + logo URL) from the CarPlates API. Called from the
/// login screen while the user types the company code, so the API caches the FwApi lookup.
/// </summary>
public class CompanyApiService(
    IHttpClientFactory httpClientFactory,
    ILoggingService loggingService) : ICompanyApiService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILoggingService _loggingService = loggingService;

    private HttpClient Client => _httpClientFactory.CreateClient("CarPlatesApi");

    public async Task<CompanyInfoResult> GetCompanyInfoAsync(string companyCode, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await Client.GetAsync(
                $"settings/company-info?companyCode={Uri.EscapeDataString(companyCode)}", cancellationToken);
            stopwatch.Stop();

            _loggingService.LogApi("settings/company-info", response.IsSuccessStatusCode, stopwatch.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
                return new CompanyInfoResult(false, null, null, "API error");

            var dto = await response.Content.ReadFromJsonAsync<CompanyInfoResponse>(ApiJsonOptions.Default, cancellationToken);
            if (dto == null)
                return new CompanyInfoResult(false, null, null, "Invalid response");

            return new CompanyInfoResult(true, dto.CompanyName, dto.LogoUrl, null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _loggingService.LogApi("settings/company-info", false, stopwatch.ElapsedMilliseconds);
            return new CompanyInfoResult(false, null, null, ex.Message);
        }
    }

    private record CompanyInfoResponse(string? CompanyName, string? LogoUrl);
}
