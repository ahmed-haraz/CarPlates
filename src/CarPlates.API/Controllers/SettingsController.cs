using CarPlates.API.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public record VerifyPasswordRequest(string CompanyCode, string Password);

    private record FwControlRaw(
        [property: JsonPropertyName("loginPassword")] string? LoginPassword);
}
