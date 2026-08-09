using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using CarPlates.Shared.Constants;
using Microsoft.Data.SqlClient;

namespace CarPlates.API.Data;

/// <summary>
/// Resolves the SQL Server connection string for the company that made the current
/// request. The company code is taken from the X-Company-Code request header (sent by
/// the mobile app on every call) and, as a fallback, from the authenticated user's
/// companyCode JWT claim.
/// <para>
/// The connection string is fetched from the external ArkanCloud FwApi
/// (FwMobileControls), which stores each company's SQL server/database credentials.
/// Results are cached in-memory for a short TTL so the FwApi is only contacted once per
/// company per TTL window instead of on every request. If the FwApi is unreachable or
/// returns no database configuration for the company, the locally configured
/// "HexaConnection" connection string is used as a fallback so the API keeps working.
/// </para>
/// </summary>
public interface ICompanyConnectionProvider
{
    /// <summary>The company code resolved for the current request.</summary>
    string CompanyCode { get; }

    /// <summary>The SQL Server connection string for that company.</summary>
    string ConnectionString { get; }
}

public class CompanyConnectionProvider(
    IHttpContextAccessor httpContextAccessor,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<CompanyConnectionProvider> logger) : ICompanyConnectionProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<CompanyConnectionProvider> _logger = logger;

    private static readonly ConcurrentDictionary<string, CachedConnection> Cache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private HttpClient Client => _httpClientFactory.CreateClient("FwApi");

    public string CompanyCode =>
        _httpContextAccessor.HttpContext?.Request.Headers[CompanyConstants.CompanyCodeHeader].FirstOrDefault()
        ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("companyCode")
        ?? AuthConstants.DefaultCompanyCode;

    public string ConnectionString
    {
        get
        {
            var companyCode = CompanyCode;

            if (Cache.TryGetValue(companyCode, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow)
            {
                return cached.Value;
            }

            var connectionString = FetchConnectionString(companyCode);
            Cache[companyCode] = new CachedConnection(connectionString, DateTimeOffset.UtcNow.Add(CacheTtl));
            return connectionString;
        }
    }

    private string FetchConnectionString(string companyCode)
    {
        try
        {
            var response = Client.GetAsync($"api/FwMobileControls/1/1/{companyCode}")
                .GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "FwApi returned {Status} for company {CompanyCode}; falling back to local connection string.",
                    (int)response.StatusCode, companyCode);
                return GetLocalFallback(companyCode);
            }

            var control = response.Content.ReadFromJsonAsync<CompanyControlRaw>(JsonOptions)
                .GetAwaiter().GetResult();

            if (control == null ||
                string.IsNullOrWhiteSpace(control.Server) ||
                string.IsNullOrWhiteSpace(control.Database) ||
                string.IsNullOrWhiteSpace(control.User) ||
                control.Password == null)
            {
                _logger.LogWarning(
                    "FwApi returned no database configuration for company {CompanyCode}; falling back to local connection string.",
                    companyCode);
                return GetLocalFallback(companyCode);
            }

            var sql = new SqlConnectionStringBuilder
            {
                DataSource = control.Server,
                InitialCatalog = control.Database,
                UserID = control.User,
                Password = control.Password,
                TrustServerCertificate = true,
                ConnectTimeout = 15
            };

            _logger.LogInformation("Resolved database connection for company {CompanyCode}", companyCode);
            return sql.ConnectionString;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to resolve connection for company {CompanyCode} from FwApi; falling back to local connection string.",
                companyCode);
            return GetLocalFallback(companyCode);
        }
    }

    private string GetLocalFallback(string companyCode)
    {
        var local = _configuration.GetConnectionString("HexaConnection");
        if (string.IsNullOrWhiteSpace(local))
        {
            throw new InvalidOperationException(
                $"Could not resolve a connection string for company '{companyCode}' and no local " +
                "'ConnectionStrings:HexaConnection' fallback is configured.");
        }

        _logger.LogInformation("Using local connection string as fallback for company {CompanyCode}", companyCode);
        return local;
    }

    private record CompanyControlRaw(
        [property: JsonPropertyName("server")] string? Server,
        [property: JsonPropertyName("sUser")] string? User,
        [property: JsonPropertyName("sDatabase")] string? Database,
        [property: JsonPropertyName("sPassword")] string? Password);

    private record CachedConnection(string Value, DateTimeOffset ExpiresAt);
}
