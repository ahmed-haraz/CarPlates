using CarPlates.API.Interface;
using CarPlates.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SettingsController(IHttpClientFactory httpClientFactory, ILogger<SettingsController> logger) : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<SettingsController> _logger = logger;

    private HttpClient Client => _httpClientFactory.CreateClient("FwApi");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Company name + logo are fetched from the FwApi once per company per TTL window
    // and cached in-memory so the login screen does not hammer the FwApi while the
    // user types a company code.
    private static readonly ConcurrentDictionary<string, CachedCompanyInfo> CompanyInfoCache = new();
    private static readonly TimeSpan CompanyInfoTtl = TimeSpan.FromMinutes(10);

    [HttpPost("verify-password")]
    [AllowAnonymous]
    public async Task<ActionResult> VerifyPassword([FromBody] VerifyPasswordRequest request)
    {
        try
        {
            var response = await Client.GetAsync($"api/FwMobileControls/1/1/{request.CompanyCode}");
            if (!response.IsSuccessStatusCode)
                return Ok(new { isValid = false, message = "Company not found" });

            var raw = await response.Content.ReadFromJsonAsync<FwControlRaw>(JsonOptions);
            if (raw == null)
                return Ok(new { isValid = false, message = "Invalid response" });

            var isValid = raw.LoginPassword == request.Password;
            return Ok(new { isValid, message = isValid ? "OK" : "Invalid password" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password verification failed");
            return Ok(new { isValid = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Returns the company's display name and logo URL for the given company code so the
    /// mobile app can show the company logo on the login and dashboard screens. Anonymous
    /// on purpose - it is called from the login screen before any authentication happens.
    /// </summary>
    [HttpGet("company-info")]
    [AllowAnonymous]
    public async Task<ActionResult> GetCompanyInfo([FromQuery] string? companyCode)
    {
        var code = string.IsNullOrWhiteSpace(companyCode) ? AuthConstants.DefaultCompanyCode : companyCode;

        try
        {
            if (CompanyInfoCache.TryGetValue(code, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow)
            {
                return Ok(new { companyName = cached.Name, logoUrl = cached.LogoUrl });
            }

            var response = await Client.GetAsync($"api/FwMobileControls/1/1/{code}");
            if (!response.IsSuccessStatusCode)
                return Ok(new { companyName = (string?)null, logoUrl = (string?)null });

            var raw = await response.Content.ReadFromJsonAsync<FwControlRaw>(JsonOptions);
            if (raw == null)
                return Ok(new { companyName = (string?)null, logoUrl = (string?)null });

            CompanyInfoCache[code] = new CachedCompanyInfo(
                raw.CoName, raw.Logo, DateTimeOffset.UtcNow.Add(CompanyInfoTtl));

            return Ok(new { companyName = raw.CoName, logoUrl = raw.Logo });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load company info for {CompanyCode}", code);
            return Ok(new { companyName = (string?)null, logoUrl = (string?)null });
        }
    }

    public record VerifyPasswordRequest(string CompanyCode, string Password);

    private record FwControlRaw(
        [property: JsonPropertyName("coName")] string? CoName,
        [property: JsonPropertyName("loginPassword")] string? LoginPassword,
        [property: JsonPropertyName("logo")] string? Logo);

    private record CachedCompanyInfo(string? Name, string? LogoUrl, DateTimeOffset ExpiresAt);
}
